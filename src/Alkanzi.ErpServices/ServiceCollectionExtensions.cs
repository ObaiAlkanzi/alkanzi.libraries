using Microsoft.Extensions.DependencyInjection;

namespace Alkanzi.ErpServices;

/// <summary>
/// DI registration for the ERP services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IErpApprovalEngine"/>, the audit interceptor it
    /// saves through, and the tenant/user providers it needs.
    /// </summary>
    /// <typeparam name="TUser">Your <see cref="IErpUserProvider"/> implementation.</typeparam>
    /// <typeparam name="TCompany">Your <see cref="IErpCompanyContext"/> implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// Assumes <see cref="ErpDbContext"/> is registered with <c>AddDbContext</c>,
    /// with the registered <see cref="ErpAuditSaveChangesInterceptor"/> attached
    /// so approvals stamp the audit columns:
    /// <code>
    /// services.AddErpApprovalEngine&lt;CurrentUser, CurrentCompany&gt;();
    /// services.AddDbContext&lt;ErpDbContext&gt;((sp, o) =&gt; o
    ///     .UseOracle(connectionString, x =&gt; x.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19))
    ///     .AddInterceptors(sp.GetRequiredService&lt;ErpAuditSaveChangesInterceptor&gt;()));
    /// </code>
    /// </remarks>
    public static IServiceCollection AddErpApprovalEngine<TUser, TCompany>(this IServiceCollection services)
        where TUser : class, IErpUserProvider
        where TCompany : class, IErpCompanyContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IErpUserProvider, TUser>();
        services.AddScoped<IErpCompanyContext, TCompany>();
        services.AddScoped(sp => new ErpAuditSaveChangesInterceptor(sp.GetRequiredService<IErpUserProvider>()));
        services.AddScoped<IErpApprovalEngine, ErpApprovalEngine>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="IErpProcedureService"/> over <see cref="ErpDbContext"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// Independent of <see cref="AddErpApprovalEngine{TUser, TCompany}"/> — a
    /// consumer that only calls procedures needs just this and a registered
    /// <see cref="ErpDbContext"/>.
    /// </remarks>
    public static IServiceCollection AddErpProcedureService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddScoped<IErpProcedureService, ErpProcedureService>();
    }
}
