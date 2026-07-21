namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// Tuning knobs for <see cref="AuditableSaveChangesInterceptor"/>.
/// </summary>
public sealed class AuditableOptions
{
    /// <summary>
    /// User id stamped when <see cref="IAuditUserProvider.GetCurrentUserId"/>
    /// returns <see langword="null"/>. Defaults to <c>0</c>.
    /// </summary>
    public int SystemUserId { get; set; }

    /// <summary>
    /// When <see langword="true"/> (the default), deleting an
    /// <see cref="IAuditable"/> entity is rewritten into an update that sets
    /// <c>IS_DELETED</c> — the row is never removed. Set to
    /// <see langword="false"/> to let deletes through while still stamping them.
    /// </summary>
    public bool SoftDelete { get; set; } = true;
}
