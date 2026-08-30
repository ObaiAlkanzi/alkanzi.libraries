using Alkanzi.Erp.Domain.Common;

namespace Alkanzi.Erp.Domain.Security;

/// <summary>
/// The top of the ownership hierarchy — the ERP's <c>ORG_ID</c>.
/// <para>
/// Organization owns companies, a company owns branches, and a transaction carries all three.
/// That is the same shape as the existing ERP's <c>TRANSACTION_BASE</c>, which stamps
/// <c>ORG_ID</c>, <c>COMP_ID</c> and <c>BRANCH_ID</c> on every document.
/// </para>
/// <para>
/// An organization is the hard tenancy boundary: data never crosses it. A company is the
/// accounting boundary — separate books, but one group that may report across them.
/// </para>
/// </summary>
public class Organization : AuditableEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public ICollection<Company> Companies { get; set; } = new List<Company>();
}
