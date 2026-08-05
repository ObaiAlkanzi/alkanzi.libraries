using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// Runs <see cref="ErpApprovalEngine"/> against the real ERP. Reads run
/// straight; every write runs inside a transaction that is never committed, so
/// the ERP's own rows are left untouched.
/// </summary>
[Collection(ErpOracleCollection.Name)]
public class ErpApprovalEngineOracleTests(ErpServicesFixture fixture)
{
    private const int Org = 21;
    private const int Comp = 6;
    private const int Branch = 1;

    // callRegistration dispatches to CALL_REGISTERATION, which is both
    // approvable and workflow-bound; id 1 is a live row.
    private const string DocType = "CreditNotes";
    private const int TransId = 201;

    private readonly ErpServicesFixture _fixture = fixture;

    private IErpApprovalEngine EngineFor(ErpDbContext context, IErpUserProvider? userProvider = null)
        => ErpServicesFixture.EngineFor(context, Org, Comp, Branch, userProvider);

    [DockerFact]
    public async Task GetTransAsyncTest()
    {
        await using var context = _fixture.CreateContext();

        var row = await EngineFor(context).GetAsync(DocType, TransId);

        Assert.NotNull(row);
        Assert.IsAssignableFrom<IErpApprovable>(row);
        Assert.IsAssignableFrom<IErpWorkflowBound>(row);
    }

    /// <summary>Acting user for <see cref="SubmitTransTest"/>.</summary>
    private const int ActingUserId = 21;

    [DockerFact]
    public async Task SubmitTransTest()
    {
        // One provider for BOTH the context and the engine: the interceptor stamps
        // UPDATED_BY from the context's, while LVL_AUTHORIZATION and the approval-log
        // row read the engine's. Two instances here would submit as two users.
        var actingUser = new StubUserProvider { UserId = ActingUserId };

        await using var context = _fixture.CreateContext(actingUser);
        await using var transaction = await context.Database.BeginTransactionAsync();

        var engine = EngineFor(context, actingUser);

        // Live row 201 is currently APPROVED, so the guard rejects Submit before the
        // user-21 branch is ever reached. Force a known pending baseline inside the
        // rolled-back transaction, exactly as Rework/Reject do.
        await ResetToPendingAsync(context, level: 0);

        var workflow = await engine.ResolveWorkflowAsync(DocType, TransId);
        Assert.NotNull(workflow);

        var result = await engine.ApplyApprovalAsync(
            DocType, TransId, ApprovalAction.Submit, targetLevel: 0,
            remarks: "test Submit 1", sgId: 1);

        Assert.NotNull(result);
        // Report the outcome, not just "false" — every non-applied outcome
        // (NotAuthorized, AlreadyApproved, NoWorkflow, ProcessFailed) looks identical
        // through a bare Assert.True on Status.
        Assert.True(result.Status, $"{result.Outcome}: {result.Message}");

        // The submit must be attributed to user 21, not the fixture's default 42 —
        // that attribution is the whole point of this test.
        var audited = Assert.IsAssignableFrom<IErpAuditable>(result.Row);
        Assert.Equal(ActingUserId, audited.UPDATED_BY);
        Assert.Equal(ActingUserId, result.Notification!.ActingUser);

        // User 21 is the engine's special case: it jumps straight to the workflow's
        // final level instead of climbing one. Reaching the final level also flips the
        // status to Approve and stamps the digital signature.
        Assert.Equal(workflow!.FinalLevel, result.Row!.APPROVE_LEVEL);
        Assert.Equal((int)ApprovalAction.Approve, result.Row.APPROVE_STATUS);

        await transaction.RollbackAsync();
    }

    [DockerFact]
    public async Task Rework()
    {
        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        // id 1 may be terminal (approved/rejected) in the live data, which the
        // guards block; force a known pending baseline first — all inside the
        // rolled-back transaction — so the transition is actually testable.
        await ResetToPendingAsync(context, level: 4);

        var result = await EngineFor(context).ReworkAsync(DocType, TransId, targetLevel: 1, sgId: 1);

        Assert.True(result.Status);
        Assert.Equal((int)ApprovalAction.Rework, result.Row!.APPROVE_STATUS);
        Assert.Equal(1, result.Row.APPROVE_LEVEL);

        await transaction.RollbackAsync();
    }

    [DockerFact]
    public async Task Reject()
    {
        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        await ResetToPendingAsync(context, level: 3);

        var result = await EngineFor(context).RejectAsync(DocType, TransId, sgId: 1);

        Assert.True(result.Status);
        Assert.Equal((int)ApprovalAction.Reject, result.Row!.APPROVE_STATUS);
        Assert.Equal(3, result.Row.APPROVE_LEVEL);   // frozen at the level it was rejected on

        await transaction.RollbackAsync();
    }

    [DockerFact]
    public async Task Submitting_stamps_the_acting_user_on_the_audit_columns()
    {
        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        await ResetToPendingAsync(context, level: 0);

        // The context carries the audit interceptor, so the engine's save
        // attributes the change without the engine knowing about auditing.
        var result = await EngineFor(context).SubmitAsync(DocType, TransId, sgId: 1);

        var audited = Assert.IsAssignableFrom<IErpAuditable>(result.Row);
        Assert.True(audited.IS_UPDATED);
        Assert.Equal(_fixture.UserProvider.UserId, audited.UPDATED_BY);
        Assert.NotNull(audited.UPDATED_AT);

        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Forces the live transaction into a known, non-terminal (submitted) state at
    /// a given level, so a transition can be asserted deterministically. Runs
    /// inside the caller's transaction, which is rolled back.
    /// </summary>
    /// <remarks>
    /// Dispatches through <c>FM_TRANSACTION_MENU</c> the same way the engine does,
    /// rather than through a fixed DbSet — otherwise this silently resets the wrong
    /// table whenever <see cref="DocType"/> changes, and the engine then reads a row
    /// this never touched. The engine reads approval columns by raw SQL with no
    /// entity type, so the reset has to be raw SQL too: a tracked-entity save would
    /// not be visible to it.
    /// </remarks>
    private static async Task ResetToPendingAsync(ErpDbContext context, int level)
    {
        var tableName = await context.TransactionMenus
            .Where(m => m.DOC_TYPE == DocType)
            .Select(m => m.TABLE_NAME)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException(
                $"No FM_TRANSACTION_MENU row with a TABLE_NAME for document type '{DocType}'.");

        var affected = await context.Database.ExecuteSqlRawAsync(
            $"UPDATE {tableName} SET APPROVE_STATUS = :p_status, APPROVE_LEVEL = :p_level WHERE ID = :p_id",
            new OracleParameter("p_status", (int)ApprovalAction.Submit),
            new OracleParameter("p_level", level),
            new OracleParameter("p_id", TransId));

        if (affected == 0)
        {
            throw new InvalidOperationException(
                $"No row with ID {TransId} in {tableName} for document type '{DocType}'.");
        }
    }
}
