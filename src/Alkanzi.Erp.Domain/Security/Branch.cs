using Alkanzi.Erp.Domain.Common;

namespace Alkanzi.Erp.Domain.Security;

/// <summary>
/// A location or operating unit within a <see cref="Company"/> — the ERP's <c>BRANCH_ID</c>.
/// <para>
/// Branch is the row-level scoping dimension: a user sees the branches their security groups
/// grant, and documents carry the branch they were raised in.
/// </para>
/// </summary>
public class Branch : AuditableEntity
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
