namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// Exercises <see cref="ErpApprovalDashboardService"/> against the real ERP:
/// resolves an accessible document type to its table and reads the approval rows,
/// enriched from the registry. Read-only — nothing is written.
/// </summary>
[Collection(ErpOracleCollection.Name)]
public class ErpApprovalDashboardOracleTests(ErpServicesFixture fixture)
{
    // callRegistration dispatches to CALL_REGISTERATION and is live.
    private const string DocType = "callRegistration";

    private readonly ErpServicesFixture _fixture = fixture;

    [DockerFact]
    public async Task GetDocTransAsync()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        var rows = await dashboard.GetDataAsync([DocType], ApprovalDashboardFilter.Rejected);
        var departments = await dashboard.GetDepartmentEmployeesAsync(0);
        Assert.NotNull(rows);
        Assert.NotEmpty(rows);   // callRegistration has live rows

        Assert.All(rows, r =>
        {
            Assert.Equal(DocType, r.DocType);   // scoped to the requested doc type
            Assert.NotEqual(0, r.Id);
            Assert.NotEqual(default, r.CreatedAt);
        });
    }

    [DockerFact]
    public async Task GetAsync_with_no_docTypes_returns_empty()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        var rows = await dashboard.GetDataAsync([]);

        Assert.Empty(rows);
    }

    [DockerFact]
    public async Task GetAsync_with_an_unconfigured_docType_returns_empty()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        // No FM_TRANSACTION_MENU row → nothing to resolve, no throw.
        var rows = await dashboard.GetDataAsync(["a-doc-type-that-is-not-configured"]);

        Assert.Empty(rows);
    }

    [DockerFact]
    public async Task GetAsync_filters_by_status()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        const int status = (int)ApprovalAction.Approve;   // 4
        var rows = await dashboard.GetDataAsync([DocType], ApprovalDashboardFilter.Approved);

        Assert.All(rows, r => Assert.Equal(status, r.ApproveStatus));
    }
}
