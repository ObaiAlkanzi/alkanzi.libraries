using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Alkanzi.ErpServices;

/// <summary>
/// DI registration for the ERP services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IErpApprovalEngine"/> and the acting-user provider it
    /// needs, over the package's <see cref="ErpDbContext"/> (a connection holder over
    /// the ERP connection string — register it with <see cref="AddErpDbContext"/>).
    /// The engine reads and updates the transaction row and all approval tables with
    /// raw SQL by table name, so it needs <b>no entity types and no link to the host's
    /// own application context</b>.
    /// </summary>
    /// <typeparam name="TUser">Your <see cref="IErpUserProvider"/> implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// Tenant (ORG/COMP/BRANCH), the initiator and the doc date all come from the
    /// transaction row (read by SQL). Runs on its own connection:
    /// <code>
    /// services.AddErpApprovalEngine&lt;CurrentUser&gt;();
    /// services.AddErpDbContext(config.GetConnectionString("Erp")!);
    /// </code>
    /// </remarks>
    public static IServiceCollection AddErpApprovalEngine<TUser>(this IServiceCollection services)
        where TUser : class, IErpUserProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IErpUserProvider, TUser>();
        services.AddScoped<IErpApprovalEngine>(sp => new ErpApprovalEngine(
            sp.GetRequiredService<ErpDbContext>(),
            userProvider: sp.GetRequiredService<IErpUserProvider>()));

        return services;
    }

    /// <summary>
    /// Registers <see cref="IErpProcedureService"/> over the package's
    /// <see cref="ErpDbContext"/> connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddErpProcedureService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddScoped<IErpProcedureService>(
            sp => new ErpProcedureService(sp.GetRequiredService<ErpDbContext>()));
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

        return services.AddScoped<IErpApprovalProcessService>(
            sp => new ErpApprovalProcessService(sp.GetRequiredService<ErpDbContext>()));
    }

    /// <summary>
    /// Registers <see cref="IErpApprovalDashboardService"/> — approval rows across the
    /// document types a user has access to, plus the department-employee panel
    /// (<c>PANEL.DEPARTMENT_EMPLOYEES</c>). Reads everything by table name with raw SQL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddErpApprovalDashboardService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddScoped<IErpApprovalDashboardService>(
            sp => new ErpApprovalDashboardService(sp.GetRequiredService<ErpDbContext>()));
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
    /// Standalone convenience for hosts that want the package's own
    /// <see cref="ErpDbContext"/> (e.g. tests). A host that runs the engine on its
    /// own context does not need this. Attaches the
    /// <see cref="ErpAuditSaveChangesInterceptor"/> here, exactly once, when one has
    /// been registered. Pin 19c yourself if you supply <paramref name="configure"/>
    /// without calling the base overload.
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

        services.AddDbContext<ErpDbContext>(ConfigureErpOptions(connectionString, configure));

        return services;
    }

    /// <summary>
    /// Registers a <b>subclass</b> of <see cref="ErpDbContext"/> as the engine's
    /// context — so you can map your own approvable tables (override
    /// <see cref="ErpDbContext.OnModelCreating"/>, call <c>base</c>, then add your
    /// entities) while keeping the Oracle 19c pin and audit interceptor wiring.
    /// The engine, dashboard and procedure services resolve <see cref="ErpDbContext"/>,
    /// so they get your <typeparamref name="TContext"/> instance and see everything
    /// it maps.
    /// </summary>
    /// <typeparam name="TContext">
    /// Your context, deriving from <see cref="ErpDbContext"/>. Its constructor must
    /// take <c>DbContextOptions&lt;ErpDbContext&gt;</c> and forward it to <c>base</c>.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The ERP connection string (see the base overload).</param>
    /// <param name="configure">Optional hook to tweak the Oracle options further.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// <code>
    /// public sealed class FlexionErpDbContext : ErpDbContext
    /// {
    ///     public FlexionErpDbContext(DbContextOptions&lt;ErpDbContext&gt; options) : base(options) { }
    ///     protected override void OnModelCreating(ModelBuilder b)
    ///     {
    ///         base.OnModelCreating(b);                 // keep the registry / log / workflow tables
    ///         b.Entity&lt;PurchaseOrderHeader&gt;(e =&gt;   // PurchaseOrderHeader : IErpApprovable, ...
    ///         {
    ///             e.HasKey(x =&gt; x.ID);
    ///             e.ToTable("PO_HDR");
    ///             e.Property(x =&gt; x.ID).ValueGeneratedNever();
    ///             e.HasQueryFilter(x =&gt; x.IS_DELETED != true);
    ///         });
    ///     }
    /// }
    ///
    /// services.AddErpApprovalEngine&lt;CurrentUser&gt;();
    /// services.AddErpDbContext&lt;FlexionErpDbContext&gt;(config.GetConnectionString("Erp")!);
    /// </code>
    /// </remarks>
    public static IServiceCollection AddErpDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        Action<OracleDbContextOptionsBuilder>? configure = null)
        where TContext : ErpDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Register TContext as the ErpDbContext service, so everything that depends
        // on ErpDbContext gets the subclass instance (and its extra mappings).
        services.AddDbContext<ErpDbContext, TContext>(ConfigureErpOptions(connectionString, configure));

        return services;
    }

    // Shared Oracle options: pin 19c before the caller's tweaks, then attach the
    // audit interceptor here (exactly once) when one has been registered — a
    // procedure-only host that never called AddErpApprovalEngine has none.
    private static Action<IServiceProvider, DbContextOptionsBuilder> ConfigureErpOptions(
        string connectionString,
        Action<OracleDbContextOptionsBuilder>? configure)
        => (sp, options) =>
        {
            options.UseOracle(connectionString, oracle =>
            {
                // 19c: the 23.x provider defaults to 23ai SQL and emits native
                // BOOLEAN, which 19c rejects (ORA-00902). Pin it before the
                // caller's own tweaks.
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
                configure?.Invoke(oracle);
            });

            var interceptor = sp.GetService<ErpAuditSaveChangesInterceptor>();
            if (interceptor is not null)
            {
                options.AddInterceptors(interceptor);
            }
        };
}
