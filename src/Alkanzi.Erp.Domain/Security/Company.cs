using Alkanzi.Erp.Domain.Common;

namespace Alkanzi.Erp.Domain.Security;

/// <summary>
/// A legal entity the ERP holds books for — the ERP's <c>COMP_ID</c>.
/// <para>
/// This is the outermost scoping boundary: every transactional record belongs to exactly one
/// company, and a user only ever works inside one at a time. Getting this on the entities
/// from day one is deliberate — retrofitting a tenant discriminator onto a live ERP is one of
/// the genuinely painful migrations.
/// </para>
/// </summary>
public class Company : AuditableEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>Reporting currency, ISO 4217.</summary>
    public string Currency { get; set; } = "AED";

    public bool IsActive { get; set; } = true;

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
