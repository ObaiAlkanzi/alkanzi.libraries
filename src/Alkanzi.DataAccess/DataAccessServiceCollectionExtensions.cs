using Alkanzi.Application.Abstractions;
using Alkanzi.DataAccess.Repositories;
using Alkanzi.DataAccess.Search;
using Alkanzi.SearchEngine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alkanzi.DataAccess;

public static class DataAccessServiceCollectionExtensions
{
    /// <summary>
    /// Wires the infrastructure layer: the SQL Server <see cref="AppDbContext"/>, the repository
    /// implementations of the application ports, and the search-engine adapters (providers).
    /// </summary>
    /// <remarks>
    /// <see cref="AppDbContext"/> is registered as <b>transient</b> on purpose: the search engine
    /// fans out to providers in parallel and EF Core's <see cref="DbContext"/> is not safe for
    /// concurrent use, so each provider needs its own instance (still disposed with the request scope).
    /// </remarks>
    public static IServiceCollection AddAlkanziDataAccess(this IServiceCollection services, string connectionString)
    {
        // One shared, stateless interceptor keeps SEARCH_INDEX live on every EF save.
        var searchIndexInterceptor = new SearchIndexInterceptor();

        services.AddDbContext<AppDbContext>(
            options => options
                .UseSqlServer(connectionString)
                .AddInterceptors(searchIndexInterceptor),
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Singleton);

        // Application ports -> EF implementations.
        services.AddScoped<IProcurementRepository, ProcurementRepository>();

        // Search engine + the unified index provider: one query over SEARCH_INDEX covers every
        // entity type (LPOs, calls, vendors, customers, …), ranked and permission-filtered.
        services.AddAlkanziSearch();
        services.AddSearchProvider<SearchIndexProvider>();

        return services;
    }
}
