using Microsoft.AspNetCore.Identity;

namespace Alkanzi.Erp.Domain.Security;

/// <summary>
/// The signed-in user. Extends ASP.NET Core Identity rather than sitting beside it, so there
/// is one user table and one identity — credentials, lockout and roles come from Identity,
/// while the ERP columns below carry the scoping the rest of the system needs.
/// <para>
/// This lives in the domain, and the reference that makes it possible is
/// <c>Microsoft.Extensions.Identity.Stores</c> — the POCO base classes only. The EF stores
/// live in a separate package that stays in the data-access layer, so the domain still has no
/// persistence dependency. The alternative, a domain <c>User</c> mirroring an infrastructure
/// <c>ApplicationUser</c>, buys purity at the cost of keeping two user records in step, which
/// is a bad trade in a system where "who is this and what may they see" is a domain question.
/// </para>
/// <para>
/// The key is <see cref="int"/>, not the Identity default of <see cref="string"/>: user ids
/// are stamped onto every audited row as <c>CREATED_BY</c>/<c>UPDATED_BY</c>, and a GUID
/// string in those columns would cost width on every table and a join on every lookup.
/// </para>
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = "";

    /// <summary>Company the user works in. Their session is scoped to it.</summary>
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>Default branch for documents this user raises. Access still comes from groups.</summary>
    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>Link to the HR record, where one exists. Null for service accounts.</summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Set false to block sign-in without deleting the account, so the user's audit trail and
    /// foreign keys stay intact.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAtUtc { get; set; }

}
