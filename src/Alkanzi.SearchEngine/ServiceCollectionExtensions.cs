using Microsoft.Extensions.DependencyInjection;

namespace Alkanzi.SearchEngine;

/// <summary>DI helpers for wiring the engine and its providers.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="ISearchEngine"/> (the default <see cref="SearchEngine"/>).</summary>
    public static IServiceCollection AddAlkanziSearch(this IServiceCollection services)
    {
        services.AddScoped<ISearchEngine, SearchEngine>();
        return services;
    }

    /// <summary>Registers a concrete <see cref="ISearchProvider"/> type.</summary>
    public static IServiceCollection AddSearchProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, ISearchProvider
    {
        services.AddScoped<ISearchProvider, TProvider>();

        return services;
    }

    /// <summary>Registers a provider built from a factory (e.g. a configured TransactionSearchProvider).</summary>
    public static IServiceCollection AddSearchProvider(
        this IServiceCollection services, Func<IServiceProvider, ISearchProvider> factory)
    {
        services.AddScoped(factory);
        return services;
    }
}
