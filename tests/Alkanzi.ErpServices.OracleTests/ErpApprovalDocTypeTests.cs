using System.Data;

namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// The same approval transition, driven across several document types. Each case
/// is a (docType, transId, tenant) the ERP actually has; dispatch resolves each
/// docType to its own table through <c>FM_TRANSACTION_MENU</c>, so one test body
/// covers them all. Every write runs inside a rolled-back transaction.
/// </summary>
/// <remarks>
/// Add a document type by adding a row to <see cref="Cases"/> — nothing else
/// changes. The reset is generic (it works through <see cref="IErpApprovable"/>),
/// so it needs no per-table DbSet.
/// </remarks>
[Collection(ErpOracleCollection.Name)]
public class ErpApprovalDocTypeTests(ErpServicesFixture fixture)
{
    private readonly ErpServicesFixture _fixture = fixture;

    /// <summary>
    /// One row per document type to cover: the type code, a live transaction id,
    /// and the tenant that row belongs to.
    /// </summary>
    public static TheoryData<string, int, int, int, int?> Cases() => new()
    {
        //  docType,            transId,  org,  comp,  branch
        { "callRegistration",   1,        21,   6,     1 },
        // { "JournalVoucher",  <live id>, 21,   6,     1 },   // FM_JOURNAL_HDR — not workflow-bound
    };

    [DockerTheory]
    [MemberData(nameof(Cases))]
    public async Task MultiDocTests(
        string docType, int transId, int org, int comp, int? branch)
    {
        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var engine = ErpServicesFixture.EngineFor(context, org, comp, branch);

        // The final-level rule couples status to level: a submit that lands on the
        // workflow's last level is forced to Approve. Resolve the workflow up front
        // so the expectation holds for any docType — including one whose final
        // level is 1, and non-workflow docs (null → no forced approval).
        var workflow = await engine.ResolveWorkflowAsync(docType, transId);

        // Known pending baseline at level 0, so Submit deterministically climbs to
        // 1 whatever state the live row happens to sit in.
        await ResetToPendingAsync(engine, context, docType, transId, level: 0);

        //var result = await engine.SubmitAsync(docType, transId);
        var result = await engine.ApplyApprovalAsync(docType, transId, ApprovalAction.Submit, sgId: 1);
        Assert.True(result.Status);
        Assert.Equal(1, result.Row!.APPROVE_LEVEL);   // climbed 0 -> 1

        var expectedStatus = workflow?.FinalLevel == 1
            ? (int)ApprovalAction.Approve             // final-level rule forces approval
            : (int)ApprovalAction.Submit;
        Assert.Equal(expectedStatus, result.Row.APPROVE_STATUS);

        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Forces the transaction into a known, non-terminal (submitted) state at a
    /// given level. Loads the row through the engine, which returns it as
    /// <see cref="IErpApprovable"/> whatever concrete table it maps to — so a
    /// single helper resets any document type. Runs inside the caller's
    /// transaction, which is rolled back.
    /// </summary>
    private static async Task ResetToPendingAsync(
        IErpApprovalEngine engine, ErpDbContext context, string docType, object transId, int level)
    {
        var row = await engine.GetAsync(docType, transId);
        Assert.NotNull(row);

        row!.APPROVE_STATUS = (int)ApprovalAction.Submit;
        row.APPROVE_LEVEL = level;
        await context.SaveChangesAsync();
    }
}
