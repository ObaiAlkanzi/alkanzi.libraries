namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// Supplies the id of the user responsible for the current unit of work.
/// Implement this against whatever carries identity in your application —
/// <c>HttpContext</c> claims in a web app, a job context in a worker, a
/// fixed system id in a migration tool.
/// </summary>
public interface IAuditUserProvider
{
    /// <summary>
    /// Returns the current user's id, or <see langword="null"/> when no user is
    /// in scope (background jobs, seeding, health checks).
    /// </summary>
    /// <remarks>
    /// Called once per <c>SaveChanges</c>, not once per entity. Returning
    /// <see langword="null"/> is not an error: the interceptor falls back to
    /// <see cref="AuditableOptions.SystemUserId"/>.
    /// </remarks>
    int? GetCurrentUserId();
}
