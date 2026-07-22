using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Registers <see cref="IEntityResolver"/> over <typeparamref name="TContext"/>.
    /// </summary>
    /// <typeparam name="TContext">The context whose model is searched.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// Takes the context type explicitly because <c>AddDbContext&lt;TContext&gt;</c>
    /// registers only <typeparamref name="TContext"/>, never the
    /// <see cref="DbContext"/> base — so the resolver cannot ask for it directly.
    /// </remarks>
    public static IServiceCollection AddEntityResolver<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddScoped<IEntityResolver>(sp => new EntityResolver(sp.GetRequiredService<TContext>()));
    }

    /// <summary>
    /// Registers <see cref="IApprovalEngine{TMenu}"/> over
    /// <typeparamref name="TContext"/>.
    /// </summary>
    /// <typeparam name="TContext">The context mapping the menu and transaction tables.</typeparam>
    /// <typeparam name="TMenu">Your document-type registry entity.</typeparam>
    /// <typeparam name="TCompany">
    /// Your <see cref="ICompanyContext"/> implementation, supplying the tenant
    /// the lookup is scoped to.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// Uses a registered <see cref="IEntityResolver"/> if there is one, so a
    /// custom implementation still wins, and otherwise builds its own. That
    /// makes this independent of whether
    /// <see cref="AddEntityResolver{TContext}"/> was called, and of the order.
    /// </remarks>
    public static IServiceCollection AddApprovalEngine<TContext, TMenu, TCompany>(this IServiceCollection services)
        where TContext : DbContext
        where TMenu : class, ITransactionMenu
        where TCompany : class, ICompanyContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICompanyContext, TCompany>();

        return services.AddScoped<IApprovalEngine<TMenu>>(sp =>
        {
            var context = sp.GetRequiredService<TContext>();
            var resolver = sp.GetService<IEntityResolver>() ?? new EntityResolver(context);

            return new ApprovalEngine<TMenu>(context, resolver, sp.GetRequiredService<ICompanyContext>());
        });
    }
}
