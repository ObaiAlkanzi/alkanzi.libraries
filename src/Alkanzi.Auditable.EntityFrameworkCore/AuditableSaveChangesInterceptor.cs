using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// Stamps every tracked <see cref="IAuditable"/> entity on the way to the
/// database, and — unless disabled — rewrites deletes as soft deletes.
/// </summary>
/// <remarks>
/// Register once per <see cref="DbContext"/> via
/// <c>optionsBuilder.AddInterceptors(...)</c>, or let
/// <see cref="ServiceCollectionExtensions.AddAuditable{TProvider}"/> wire it up.
/// The interceptor is stateless and safe to share across contexts.
/// </remarks>
public sealed class AuditableSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditUserProvider _userProvider;
    private readonly AuditableOptions _options;

    /// <summary>Creates an interceptor using default <see cref="AuditableOptions"/>.</summary>
    /// <param name="userProvider">Supplies the acting user id.</param>
    public AuditableSaveChangesInterceptor(IAuditUserProvider userProvider)
        : this(userProvider, new AuditableOptions())
    {
    }

    /// <summary>Creates an interceptor with explicit options.</summary>
    /// <param name="userProvider">Supplies the acting user id.</param>
    /// <param name="options">Soft-delete and fallback-user behaviour.</param>
    public AuditableSaveChangesInterceptor(IAuditUserProvider userProvider, AuditableOptions options)
    {
        _userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var userId = _userProvider.GetCurrentUserId() ?? _options.SystemUserId;

        // Materialised up front: soft-deleting flips entries to Modified, which
        // mutates the change tracker while we are walking it.
        var entries = context.ChangeTracker
            .Entries<IAuditable>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.MarkCreated(userId);
                    break;

                case EntityState.Modified:
                    entry.Entity.MarkUpdated(userId);
                    break;

                case EntityState.Deleted when _options.SoftDelete:
                    SoftDelete(entry, userId);
                    break;

                case EntityState.Deleted:
                    entry.Entity.MarkDeleted(userId);
                    break;
            }
        }
    }

    private static void SoftDelete(EntityEntry<IAuditable> entry, int userId)
    {
        // Modified rather than Unchanged-plus-flags: the entity may carry other
        // pending edits made in the same unit of work, and those should persist.
        entry.State = EntityState.Modified;
        entry.Entity.MarkDeleted(userId);
    }
}
