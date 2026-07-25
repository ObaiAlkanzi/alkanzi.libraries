using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Alkanzi.ErpServices;

/// <summary>
/// Stamps <see cref="IErpAuditable"/> rows on save and turns deletes into soft
/// deletes — the project's own, so it depends on nothing outside it.
/// </summary>
/// <remarks>
/// Timestamps are UTC. A delete is rewritten to an update that sets
/// <c>IS_DELETED</c>, so the row stays and the global query filter hides it.
/// </remarks>
public sealed class ErpAuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IErpUserProvider _userProvider;

    /// <summary>Creates the interceptor over the given user provider.</summary>
    public ErpAuditSaveChangesInterceptor(IErpUserProvider userProvider)
        => _userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
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

        var userId = _userProvider.GetCurrentUserId();
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IErpAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CREATED_BY = userId ?? 0;
                    entry.Entity.CREATED_AT = now;
                    entry.Entity.IS_UPDATED = false;
                    entry.Entity.IS_DELETED = false;
                    break;

                case EntityState.Modified:
                    entry.Entity.UPDATED_BY = userId;
                    entry.Entity.UPDATED_AT = now;
                    entry.Entity.IS_UPDATED = true;
                    break;

                case EntityState.Deleted:
                    // Keep the row; mark it deleted instead.
                    entry.State = EntityState.Modified;
                    entry.Entity.IS_DELETED = true;
                    entry.Entity.DELETED_BY = userId;
                    entry.Entity.DELETED_AT = now;
                    break;
            }
        }
    }
}
