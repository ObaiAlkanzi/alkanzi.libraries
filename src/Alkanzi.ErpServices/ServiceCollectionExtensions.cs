using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Alkanzi.ErpServices;

/// <summary>
/// DI registration for the ERP services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IErpApprovalEngine"/>, the audit interceptor it
    /// saves through, and the acting-user provider it needs.
    /// </summary>
    /// <typeparam name="TUser">Your <see cref="IErpUserProvider"/> implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// Tenant (ORG/COMP/BRANCH) comes from the transaction row itself
    /// (<see cref="IErpTenantScoped"/>), so no company context is registered here.
    /// Assumes <see cref="ErpDbContext"/> is registered with <c>AddDbContext</c>,
    /// with the registered <see cref="ErpAuditSaveChangesInterceptor"/> attached
    /// so approvals stamp the audit columns:
    /// <code>
    /// services.AddErpApprovalEngine&lt;CurrentUser&gt;();
    /// services.AddDbContext&lt;ErpDbContext&gt;((sp, o) =&gt; o
    ///     .UseOracle(connectionString, x =&gt; x.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19))
    ///     .AddInterceptors(sp.GetRequiredService&lt;ErpAuditSaveChangesInterceptor&gt;()));
    /// </code>
    /// </remarks>
    public static IServiceCollection AddErpApprovalEngine<TUser>(this IServiceCollection services)
        where TUser : class, IErpUserProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IErpUserProvider, TUser>();
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
    /// Independent of <see cref="AddErpApprovalEngine{TUser}"/> — a
    /// consumer that only calls procedures needs just this and a registered
    /// <see cref="ErpDbContext"/>.
    /// </remarks>
    public static IServiceCollection AddErpProcedureService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddScoped<IErpProcedureService, ErpProcedureService>();
    }

    /// <summary>
    /// Registers <see cref="IErpApprovalProcessService"/> — the wrapper over the ERP's
    /// <c>SM_APPROVE_PROCESS</c> / <c>SM_REJECT_PROCESS</c> approval procedures.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddErpApprovalProcessService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddScoped<IErpApprovalProcessService, ErpApprovalProcessService>();
    }

    /// <summary>
    /// Registers <see cref="ErpDbContext"/> against the ERP's own connection
    /// string — independent of any other <c>DbContext</c> or connection the host
    /// API already uses — pinned to Oracle 19c SQL compatibility, with the audit
    /// interceptor attached when one is registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">
    /// The ERP connection string. Keep it separate from the host's other
    /// connections (e.g. a named <c>"Erp"</c> entry) — the approval engine's
    /// status change, its <c>UPDATE_SENTENCE</c>, and the approval-log writes all
    /// share this context's single connection, so they commit or roll back as one.
    /// </param>
    /// <param name="configure">Optional hook to tweak the Oracle options further.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// Attaches the <see cref="ErpAuditSaveChangesInterceptor"/> here, exactly once,
    /// when it has been registered (<see cref="AddErpApprovalEngine{TUser}"/>
    /// registers it). Do not also pass the interceptor through the context
    /// constructor — attaching it twice stamps every row twice. Pin 19c yourself if
    /// you supply <paramref name="configure"/> without calling the base overload.
    /// <code>
    /// services.AddErpApprovalEngine&lt;CurrentUser&gt;();
    /// services.AddErpProcedureService();
    /// services.AddErpDbContext(config.GetConnectionString("Erp")!);
    /// </code>
    /// </remarks>
    public static IServiceCollection AddErpDbContext(
        this IServiceCollection services,
        string connectionString,
        Action<OracleDbContextOptionsBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<ErpDbContext>((sp, options) =>
        {
            options.UseOracle(connectionString, oracle =>
            {
                // 19c: the 23.x provider defaults to 23ai SQL and emits native
                // BOOLEAN, which 19c rejects (ORA-00902). Pin it before the
                // caller's own tweaks.
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
                configure?.Invoke(oracle);
            });

            // Attached here, not via the constructor, so it lands exactly once.
            // Optional: a procedure-only host that never called
            // AddErpApprovalEngine has no interceptor to attach.
            var interceptor = sp.GetService<ErpAuditSaveChangesInterceptor>();
            if (interceptor is not null)
            {
                options.AddInterceptors(interceptor);
            }
        });

        return services;
    }
}
