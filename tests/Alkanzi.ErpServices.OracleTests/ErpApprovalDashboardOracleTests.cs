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

    // The user from userTrans.md — has levels across many workflow forms.
    private const int UserId = 2144;
    //private const int UserId = 2683;

    [DockerFact]
    public async Task GetUserScopeAsync_resolves_the_users_levels()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        var scope = await dashboard.GetUserScopeAsync(UserId);

        Assert.NotEmpty(scope);
        Assert.All(scope, s =>
        {
            Assert.NotEqual(0, s.FormId);
            Assert.False(string.IsNullOrWhiteSpace(s.DocType));
        });

        // The same (form, level) reaches a user through several security groups; the
        // query collapses them, so the pairs must be unique.
        var pairs = scope.Select(s => (s.FormId, s.LevelId)).ToList();
        Assert.Equal(pairs.Count, pairs.Distinct().Count());
    }

    [DockerFact]
    public async Task GetUserDataAsyncTest()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        var scope = await dashboard.GetUserScopeAsync(UserId);
        var allowed = scope.Select(s => (s.FormId, s.LevelId)).ToHashSet();
        //2683
        var rows = await dashboard.GetUserDataAsync(21);
        Assert.NotEmpty(rows); 
    }
    [DockerFact]
    public async Task GetUserSecurityGroupsAsyncTest()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        //var scope = await dashboard.GetUserScopeAsync(UserId);
        //var allowed = scope.Select(s => (s.FormId, s.LevelId)).ToHashSet();

        var rows = await dashboard.GetUserSecurityGroupsAsync(21);
        Assert.NotEmpty(rows);
    }
    [DockerFact]
    public async Task GetUserDataAsync_for_an_unknown_user_returns_empty()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        // Not -1: the ERP really has a USER_ID = -1 row in
        // SM_DIVISION_SECURITY_GROUPS_USERS granting 66 (form, level) pairs, so -1 is
        // a live user as far as this query is concerned, not an absent one.
        const int unknownUser = -999_999;

        Assert.Empty(await dashboard.GetUserScopeAsync(unknownUser));
        Assert.Empty(await dashboard.GetUserDataAsync(unknownUser));
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

    [DockerFact]
    public async Task GetEmployeeDataByUserIdAsync_returns_the_employee_behind_a_user()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        var employee = await dashboard.GetEmployeeDataByUserIdAsync(UserId);

        Assert.NotNull(employee);
        Assert.Equal(UserId, employee.UserId);
        Assert.NotEqual(0, employee.Id);
        Assert.False(string.IsNullOrWhiteSpace(employee.Employee));
        Assert.False(string.IsNullOrWhiteSpace(employee.DepartmentName));
        Assert.StartsWith("https://erp.fakhruddin.ae:400/files/HR/Employees/E-", employee.Profile);
    }

    [DockerFact]
    public async Task GetEmployeeDataByEmpIdAsync_returns_the_same_row_as_the_user_lookup()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        var byUser = await dashboard.GetEmployeeDataByUserIdAsync(UserId);
        Assert.NotNull(byUser);

        var byEmp = await dashboard.GetEmployeeDataByEmpIdAsync(byUser.Id);

        Assert.Equal(byUser, byEmp);   // records compare by value
    }

    [DockerFact]
    public async Task GetEmployeeDataAsync_for_an_unknown_id_returns_null()
    {
        await using var context = _fixture.CreateContext();
        var dashboard = new ErpApprovalDashboardService(context);

        Assert.Null(await dashboard.GetEmployeeDataByUserIdAsync(-999_999));
        Assert.Null(await dashboard.GetEmployeeDataByEmpIdAsync(-999_999));
    }
}
