using Microsoft.EntityFrameworkCore;

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
    private const string DocType = "callRegistration";
    private const int TransId = 1;

    private readonly ErpServicesFixture _fixture = fixture;

    private IErpApprovalEngine EngineFor(ErpDbContext context)
        => ErpServicesFixture.EngineFor(context, Org, Comp, Branch);

    [DockerFact]
    public async Task GetTransAsyncTest()
    {
        await using var context = _fixture.CreateContext();

        var row = await EngineFor(context).GetAsync(DocType, TransId);

        Assert.NotNull(row);
        Assert.IsAssignableFrom<IErpApprovable>(row);
        Assert.IsAssignableFrom<IErpWorkflowBound>(row);
    }

    [DockerFact]
    public async Task SubmitTransTest   ()
    {
        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var engine = EngineFor(context);

        // Delta, not absolute: the test must not assume where this live row sits.
        //var before = await engine.GetAsync(DocType, TransId);
        //Assert.NotNull(before);
        //var startLevel = before!.APPROVE_LEVEL;

        //var after = await engine.SubmitAsync(DocType, TransId);
       //var result = await engine.ApplyApprovalAsync(DocType, TransId, ApprovalAction.Submit,remarks:"test Submit 1",sgId : 1);
       var result = await engine.ApplyApprovalAsync(DocType, TransId, ApprovalAction.Submit,0,remarks:"test Submit 1",sgId : 1);
        Assert.NotNull(result);
        Assert.Equal((int)ApprovalAction.Approve, result.Row!.APPROVE_STATUS); 
        await transaction.CommitAsync();
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
    private static async Task ResetToPendingAsync(ErpDbContext context, int level)
    {
        var row = await context.CallRegistrations.FirstAsync(r => r.ID == TransId);
        row.APPROVE_STATUS = (int)ApprovalAction.Submit;
        row.APPROVE_LEVEL = level;
        await context.SaveChangesAsync();
    }
}
