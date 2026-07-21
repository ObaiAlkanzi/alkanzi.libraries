using Microsoft.Extensions.DependencyInjection;

namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// DI registration for the audit interceptor.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TProvider"/> as the
    /// <see cref="IAuditUserProvider"/> and adds
    /// <see cref="AuditableSaveChangesInterceptor"/> to the container.
    /// </summary>
    /// <typeparam name="TProvider">Your user-id provider implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional soft-delete / fallback-user tuning.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// Registered scoped, because a provider usually reads per-request state.
    /// The interceptor still has to be attached to the context — pass the
    /// service provider through <c>AddDbContext</c>:
    /// <code>
    /// services.AddAuditable&lt;HttpAuditUserProvider&gt;();
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =&gt; options
    ///     .UseOracle(connectionString) // any provider; the interceptor emits no SQL
    ///     .AddInterceptors(sp.GetRequiredService&lt;AuditableSaveChangesInterceptor&gt;()));
    /// </code>
    /// </remarks>
    public static IServiceCollection AddAuditable<TProvider>(
        this IServiceCollection services,
        Action<AuditableOptions>? configure = null)
        where TProvider : class, IAuditUserProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new AuditableOptions();
        configure?.Invoke(options);

        services.AddScoped<IAuditUserProvider, TProvider>();
        services.AddSingleton(options);
        services.AddScoped(sp => new AuditableSaveChangesInterceptor(
            sp.GetRequiredService<IAuditUserProvider>(),
            options));

        return services;
    }
}
