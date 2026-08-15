using Alkanzi.SearchEngine;
using Alkanzi.SearchEngine.Erp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alkanzi.SearchEngine.Erp.OracleTests;

/// <summary>
/// Live end-to-end checks against the ERP Oracle DB. Supply the connection via the
/// <c>ALKANZI_ORACLE_CONNECTION</c> env var; the tests no-op (pass) when it is absent
/// so CI without a database stays green. Read-only — no writes.
/// </summary>
public class ErpSearchOracleTests
{
    private static string? Conn => Environment.GetEnvironmentVariable("ALKANZI_ORACLE_CONNECTION");

    private static ServiceProvider Build(string conn)
    {
        var services = new ServiceCollection();
        services.AddErpSearch(conn);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Finds_a_real_inventory_lpo_by_id()
    {
        var conn = Conn;
        if (string.IsNullOrWhiteSpace(conn)) return; // skip without a DB

        using var sp = Build(conn);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpSearchDbContext>();

        var lpo = await db.IM_PURCHASE_ORDERS
            .Where(x => !x.IS_DELETED && x.DOC_TYPE == "imPurchaseOrder")
            .OrderByDescending(x => x.ID)
            .FirstOrDefaultAsync();
        if (lpo is null) return; // no data to assert against

        var engine = scope.ServiceProvider.GetRequiredService<ISearchEngine>();
        var result = await engine.SearchAsync(new SearchQuery { Term = lpo.ID.ToString() }, SearchScope.All);

        Assert.Contains(result.Hits, h => h.EntityType == "inventory" && h.Id == lpo.ID);
    }

    [Fact]
    public async Task Finds_a_real_call_by_id()
    {
        var conn = Conn;
        if (string.IsNullOrWhiteSpace(conn)) return;

        using var sp = Build(conn);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpSearchDbContext>();

        var call = await db.CALL_REGISTERATION
            .Where(x => !x.IS_DELETED)
            .OrderByDescending(x => x.ID)
            .FirstOrDefaultAsync();
        if (call is null) return;

        var engine = scope.ServiceProvider.GetRequiredService<ISearchEngine>();
        var result = await engine.SearchAsync(new SearchQuery { Term = call.ID.ToString() }, SearchScope.All);

        Assert.Contains(result.Hits, h => h.EntityType == "call" && h.Id == call.ID);
    }

    [Fact]
    public async Task Finds_calls_by_client_name()
    {
        var conn = Conn;
        if (string.IsNullOrWhiteSpace(conn)) return;

        using var sp = Build(conn);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpSearchDbContext>();

        var call = await db.CALL_REGISTERATION
            .Where(x => !x.IS_DELETED && x.NAME != null && x.NAME.Length >= 4)
            .OrderByDescending(x => x.ID)
            .FirstOrDefaultAsync();
        if (call is null) return;

        var fragment = call.NAME.Trim().Substring(0, 4);
        var engine = scope.ServiceProvider.GetRequiredService<ISearchEngine>();
        var result = await engine.SearchAsync(new SearchQuery { Term = fragment }, SearchScope.All);

        Assert.Contains(result.Hits, h => h.EntityType == "call");
    }
}
