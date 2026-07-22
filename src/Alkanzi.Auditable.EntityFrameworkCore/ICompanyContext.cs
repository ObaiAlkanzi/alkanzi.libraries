namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// Supplies the organisation, company and branch the current unit of work
/// belongs to.
/// </summary>
/// <remarks>
/// Document-type configuration is held per tenant, so a document type alone does
/// not identify one configuration — the same code is registered separately for
/// every company and branch. Implement this against whatever carries tenancy in
/// your application, the same way <see cref="IAuditUserProvider"/> carries the
/// acting user.
/// </remarks>
public interface ICompanyContext
{
    /// <summary>Current organisation id.</summary>
    int ORG_ID { get; }

    /// <summary>Current company id.</summary>
    int COMP_ID { get; }

    /// <summary>
    /// Current branch id, or <see langword="null"/> to leave the lookup
    /// unscoped by branch — which may then match more than one configuration.
    /// </summary>
    int? BRANCH_ID { get; }
}
