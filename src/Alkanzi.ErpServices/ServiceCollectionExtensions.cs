using Alkanzi.Auditable.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alkanzi.ErpServices;

/// <summary>
/// DI registration for the ERP services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IErpApprovalService"/> and the
    /// <see cref="IApprovalEngine{TMenu}"/> it needs, over
    /// <see cref="ErpDbContext"/> and <typeparamref name="TCompany"/>.
    /// </summary>
    /// <typeparam name="TCompany">
    /// Your <see cref="ICompanyContext"/> implementation, supplying the tenant
    /// approvals are scoped to.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// Assumes <see cref="ErpDbContext"/> is registered with <c>AddDbContext</c>
    /// and, for approvals to be attributable, that the audit interceptor is
    /// attached to it — see <c>AddAuditable</c> in the EF Core package.
    /// <code>
    /// services.AddAuditable&lt;HttpAuditUserProvider&gt;();
    /// services.AddDbContext&lt;ErpDbContext&gt;((sp, o) =&gt; o
    ///     .UseOracle(connectionString)
    ///     .AddInterceptors(sp.GetRequiredService&lt;AuditableSaveChangesInterceptor&gt;()));
    /// services.AddErpApprovalService&lt;CurrentCompany&gt;();
    /// </code>
    /// </remarks>
    public static IServiceCollection AddErpApprovalService<TCompany>(this IServiceCollection services)
        where TCompany : class, ICompanyContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApprovalEngine<ErpDbContext, FM_TRANSACTION_MENU, TCompany>();
        services.AddScoped<IErpApprovalService, ErpApprovalService>();

        return services;
    }
}
