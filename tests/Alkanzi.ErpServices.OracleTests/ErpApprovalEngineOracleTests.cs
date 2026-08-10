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
    private const string DocType = "imPurchaseOrder";
    private const int TransId = 2747;

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
    private const int ActingUserId = 1;

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

        // Live row 2747 is APPROVED, so with this commented out the guard returns
        // AlreadyApproved and the submit below never runs. Uncomment to force a known
        // pending baseline inside the rolled-back transaction, as Rework/Reject do.
       // await ResetToPendingAsync(engine, context, level: 0);

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
        
        Assert.Equal((int)ApprovalAction.Approve, result.Row.APPROVE_STATUS);
        await transaction.RollbackAsync();
    }

    [DockerFact]
    public async Task A_refused_approval_undoes_its_row_update_inside_a_caller_transaction()
    {
        // CreditNotes maps to MAIN_DOC_TYPE 'ManualNotes', which SM_APPROVE_PROCESS has
        // no branch for at status 4 — and user 21 jumps to FinalLevel, which sets
        // status 4. So this pair is reliably refused with ORA-20111.
        const string refusedDocType = "CreditNotes";
        const int refusedTransId = 162;

        var actingUser = new StubUserProvider { UserId = 21 };
        await using var context = _fixture.CreateContext(actingUser);

        // The CALLER owns the transaction. The engine must not roll it back — that
        // would discard the caller's work — so it has to undo its own row UPDATE via a
        // savepoint. Without one, the refused transition stays pending here and the
        // caller's next commit would persist a failed approval.
        await using var transaction = await context.Database.BeginTransactionAsync();

        var engine = EngineFor(context, actingUser);

        var before = await engine.GetAsync(refusedDocType, refusedTransId);
        Assert.NotNull(before);

        var result = await engine.ApplyApprovalAsync(
            refusedDocType, refusedTransId, ApprovalAction.Submit, targetLevel: 0, sgId: 1);

        Assert.Equal(ApprovalOutcome.ProcessFailed, result.Outcome);

        var after = await engine.GetAsync(refusedDocType, refusedTransId);
        Assert.NotNull(after);
        Assert.Equal(before!.APPROVE_STATUS, after!.APPROVE_STATUS);
        Assert.Equal(before.APPROVE_LEVEL, after.APPROVE_LEVEL);

        await transaction.RollbackAsync();
    }

    [DockerFact]
    public async Task Rework()
    {
        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var engine = EngineFor(context);

        // The row may be terminal (approved/rejected) in the live data, which the
        // guards block; force a known pending baseline first — all inside the
        // rolled-back transaction — so the transition is actually testable.
        await ResetToPendingAsync(engine, context, level: 4);

        var result = await engine.ReworkAsync(DocType, TransId, targetLevel: 1, sgId: 1);

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

        var engine = EngineFor(context);

        await ResetToPendingAsync(engine, context, level: 3);

        var result = await engine.RejectAsync(DocType, TransId, sgId: 1);

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

        var engine = EngineFor(context);

        await ResetToPendingAsync(engine, context, level: 0);

        // The context carries the audit interceptor, so the engine's save
        // attributes the change without the engine knowing about auditing.
        var result = await engine.SubmitAsync(DocType, TransId, sgId: 1);

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
    /// Resolves the table through the engine's own <see cref="IErpApprovalEngine.GetMenuAsync"/>
    /// rather than querying <c>FM_TRANSACTION_MENU</c> directly. The engine dispatches
    /// via <c>APPROVAL_REVERT_PAK.GET_TRANS_WF</c>, and a document type can have several
    /// registry rows — an unordered <c>FirstOrDefault</c> over the table is free to pick
    /// a different one, which resets a table the engine never reads and leaves the test
    /// seeing the untouched live state. The engine reads approval columns by raw SQL
    /// with no entity type, so the reset has to be raw SQL too: a tracked-entity save
    /// would not be visible to it.
    /// </remarks>
    private static async Task ResetToPendingAsync(IErpApprovalEngine engine, ErpDbContext context, int level)
    {
        var menu = await engine.GetMenuAsync(DocType)
            ?? throw new InvalidOperationException(
                $"No registry row for document type '{DocType}'.");

        var tableName = menu.TABLE_NAME
            ?? throw new InvalidOperationException(
                $"Document type '{DocType}' has no TABLE_NAME.");

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
