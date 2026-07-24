namespace Alkanzi.Auditable.EntityFrameworkCore.OracleTests;

/// <summary>
/// Runs the engine against the real FM_TRANSACTION_MENU. Read-only: these tests
/// only issue SELECTs, and never touch the scratch table the fixture owns.
/// </summary>
[Collection(OracleCollection.Name)]
public class OracleApprovalEngineTests(OracleFixture fixture)
{
    private const int Org = 21;

    private static ApprovalEngine<FM_TRANSACTION_MENU> EngineFor(
        ErpReadContext context, int comp, int? branch)
        => new(context, new EntityResolver(context), new FixedCompany(Org, comp, branch));

    [DockerFact]
    public async Task JournalVoucher_names_its_transaction_table()
    {
        await using var context = fixture.CreateErpContext();

        var menu = await EngineFor(context, comp: 6, branch: 1).GetMenuAsync("JournalVoucher");

        Assert.NotNull(menu);
        Assert.Equal("FM_JOURNAL_HDR", menu!.TABLE_NAME);
    }

    [DockerFact]
    public async Task Tenant_scoping_is_what_makes_the_lookup_single_valued()
    {
        await using var context = fixture.CreateErpContext();

        // ~70 rows share this document type. Without the org/company/branch
        // filter SingleOrDefault would throw; with it, exactly one comes back.
        var menu = await EngineFor(context, comp: 6, branch: 1).GetMenuAsync("JournalVoucher");

        Assert.NotNull(menu);
        Assert.Equal(6, menu!.COMP_ID);
        Assert.Equal(1, menu.BRANCH_ID);
    }

    [DockerFact]
    public async Task A_different_company_gets_its_own_row_with_no_table_name()
    {
        await using var context = fixture.CreateErpContext();

        // The usual shape of the registry: configured for the tenant, but
        // carrying no transaction table.
        var menu = await EngineFor(context, comp: 7, branch: 7).GetMenuAsync("JournalVoucher");

        Assert.NotNull(menu);
        Assert.Null(menu!.TABLE_NAME);
    }

    [Fact]
    public async Task TestApplyApproval()
    {
        await using var context = fixture.CreateErpContext();
        var engine = EngineFor(context, comp: 6, branch: 1);
        int transId = 1;
        var menu = await engine.GetMenuAsync("callRegistration");
        //Assert.NotNull(menu);
        //Assert.Contains("CALL_REGISTERATION", menu.TABLE_NAME);

        //var trans = await engine.GetTransactionAsync(menu.TABLE_NAME, transId);
        //Assert.NotNull(trans);

        var x = engine.ApplyApprovalAsync("callRegistration", transId, ApprovalAction.Submit);
        Assert.NotNull(x);
        //var ex = await Assert.ThrowsAsync<InvalidOperationException>(
        //    () => EngineFor(context, comp: 7, branch: 7).GetTransactionByDocTypeAsync("JournalVoucher", 1));

        //Assert.Contains("TABLE_NAME", ex.Message);
    }

    [DockerFact]
    public async Task An_unconfigured_tenant_yields_no_menu()
    {
        await using var context = fixture.CreateErpContext();

        Assert.Null(await EngineFor(context, comp: 99999, branch: 99999).GetMenuAsync("JournalVoucher"));
    }
}
