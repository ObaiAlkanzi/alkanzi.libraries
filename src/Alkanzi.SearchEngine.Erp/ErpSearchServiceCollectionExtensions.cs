using Alkanzi.SearchEngine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modules_DataTables.CALL_MODULES;
using Modules_DataTables.IM_MODULES;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Alkanzi.SearchEngine.Erp;

/// <summary>DI helpers to register the ERP entity search providers.</summary>
public static class ErpSearchServiceCollectionExtensions
{
    /// <summary>
    /// One-call setup: registers the self-contained <see cref="ErpSearchDbContext"/> against
    /// Oracle, the search engine, and the ERP providers. Just pass the connection string.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddErpSearch(config.GetConnectionString("Oracle"));
    /// // then inject ISearchEngine and call SearchAsync(...)
    /// </code>
    /// </example>
    public static IServiceCollection AddErpSearch(this IServiceCollection services, string oracleConnectionString)
    {
        if (string.IsNullOrWhiteSpace(oracleConnectionString))
            throw new ArgumentException("Oracle connection string is required", nameof(oracleConnectionString));

        services.AddDbContext<ErpSearchDbContext>(options =>
            options.UseOracle(oracleConnectionString,
                oracle => oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)));

        services.AddAlkanziSearch();
        services.AddErpSearchProviders(
            lpos:  sp => sp.GetRequiredService<ErpSearchDbContext>().IM_PURCHASE_ORDERS,
            calls: sp => sp.GetRequiredService<ErpSearchDbContext>().CALL_REGISTERATION);

        return services;
    }

    /// <summary>
    /// Registers the inventory-LPO and call providers. The callers supply how to reach each
    /// queryable set from the request scope (typically the DbContext's DbSet).
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddAlkanziSearch()
    ///         .AddErpSearchProviders(
    ///             lpos:  sp => sp.GetRequiredService&lt;FakhruddinAppDbContext&gt;().IM_PURCHASE_ORDERS,
    ///             calls: sp => sp.GetRequiredService&lt;FakhruddinAppDbContext&gt;().CALL_REGISTERATION);
    /// </code>
    /// </example>
    public static IServiceCollection AddErpSearchProviders(
        this IServiceCollection services,
        Func<IServiceProvider, IQueryable<IM_PURCHASE_ORDERS>> lpos,
        Func<IServiceProvider, IQueryable<CALL_REGISTERATION>> calls)
    {
        services.AddSearchProvider(sp => new PurchaseOrderSearchProvider(() => lpos(sp)));
        services.AddSearchProvider(sp => new CallRegistrationSearchProvider(() => calls(sp)));
        return services;
    }
}
