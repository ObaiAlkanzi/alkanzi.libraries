using Microsoft.AspNetCore.Identity;

namespace Alkanzi.Erp.Domain.Security;

/// <summary>
/// An Identity role — the unit of authorisation in this system.
/// <para>
/// Roles are assigned through Identity's own <c>AspNetUserRoles</c>, and access is decided
/// with <c>[Authorize(Roles = …)]</c> and <c>User.IsInRole(…)</c>. Nothing else grants
/// rights: there is deliberately no parallel permission or group model to keep in step with
/// this one.
/// </para>
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
    public string? Description { get; set; }

    /// <summary>Built-in roles cannot be renamed or deleted by an administrator.</summary>
    public bool IsSystemRole { get; set; }
}
