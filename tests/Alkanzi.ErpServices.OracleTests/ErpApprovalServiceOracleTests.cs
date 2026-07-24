using Alkanzi.Auditable;
using Alkanzi.Auditable.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// Runs <see cref="ErpApprovalService"/> against the real ERP. Reads run
/// straight; every write runs inside a transaction that is never committed, so
/// the ERP's own rows are left untouched.
/// </summary>
public class ErpApprovalServiceOracleTests
{
    private const int Org = 21;
    private const int Comp = 6;
    private const int Branch = 1;

    // JournalVoucher dispatches to FM_JOURNAL_HDR; id 1 is a live row.
    private const string DocType = "JournalVoucher";
    private const int TransId = 1;

    private readonly ErpServicesFixture _fixture = new();

    private IErpApprovalService ServiceFor(ErpDbContext context)
        => ErpServicesFixture.ServiceFor(context, Org, Comp, Branch);

    [DockerFact]
    public async Task Get_returns_the_document_typed_as_approvable()
    {
        await using var context = _fixture.CreateContext();

        var row = await ServiceFor(context).GetAsync(DocType, TransId);

        Assert.NotNull(row);
        Assert.IsAssignableFrom<IApprovable>(row);
    }

    [DockerFact]
    public async Task Submit_climbs_a_level_and_records_the_submitted_status()
    {
        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var service = ServiceFor(context);

        // Delta, not absolute: the test must not assume where this live row sits.
        var before = await service.GetAsync(DocType, TransId);
        Assert.NotNull(before);
        var startLevel = before!.APPROVE_LEVEL;

        var after = await service.SubmitAsync(DocType, TransId);

        Assert.Equal((int)ApprovalAction.Submit, after.APPROVE_STATUS);
        Assert.Equal(startLevel + 1, after.APPROVE_LEVEL);

        await transaction.RollbackAsync();
    }

    [DockerFact]
    public async Task Rework_drops_to_the_level_it_is_sent_back_to()
    {
        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var after = await ServiceFor(context).ReworkAsync(DocType, TransId, targetLevel: 1);

        Assert.Equal((int)ApprovalAction.Rework, after.APPROVE_STATUS);
        Assert.Equal(1, after.APPROVE_LEVEL);

        await transaction.RollbackAsync();
    }

    [DockerFact]
    public async Task Reject_freezes_the_level_and_records_the_rejected_status()
    {
        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var service = ServiceFor(context);

        var before = await service.GetAsync(DocType, TransId);
        Assert.NotNull(before);
        var startLevel = before!.APPROVE_LEVEL;

        var after = await service.RejectAsync(DocType, TransId);

        Assert.Equal((int)ApprovalAction.Reject, after.APPROVE_STATUS);
        Assert.Equal(startLevel, after.APPROVE_LEVEL);

        await transaction.RollbackAsync();
    }

    [DockerFact]
    public async Task Submitting_stamps_the_acting_user_on_the_audit_columns()
    {
        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        // The context carries the audit interceptor, so the service's save
        // attributes the change without the service knowing about auditing.
        var after = await ServiceFor(context).SubmitAsync(DocType, TransId);

        var audited = Assert.IsAssignableFrom<IAuditable>(after);
        Assert.True(audited.IS_UPDATED);
        Assert.Equal(_fixture.UserProvider.UserId, audited.UPDATED_BY);
        Assert.NotNull(audited.UPDATED_AT);

        await transaction.RollbackAsync();
    }
}
