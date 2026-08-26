using Alkanzi.Application.Abstractions;
using Alkanzi.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Alkanzi.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application-layer use-case services. Infrastructure is wired separately.</summary>
    public static IServiceCollection AddAlkanziApplication(this IServiceCollection services)
    {
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IProcurementService, ProcurementService>();
        return services;
    }
}
