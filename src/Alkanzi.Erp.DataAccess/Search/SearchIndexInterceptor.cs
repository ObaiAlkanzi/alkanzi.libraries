using System.Collections.Concurrent;
using Alkanzi.Erp.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Alkanzi.Erp.DataAccess.Search;

/// <summary>
/// Keeps <c>search_documents</c> in step with the rows it describes, on every save.
/// <para>
/// This closes a gap worth naming precisely, because it is easy to talk past. PostgreSQL
/// guarantees that <c>search_vector</c> matches the <c>search_documents</c> row it lives on —
/// that part genuinely cannot drift. It guarantees nothing about whether a
/// <c>search_documents</c> row exists at all, or still resembles the vendor or purchase order
/// it was projected from. Without this interceptor the index was only ever written by the
/// development seeder, so anything created or edited through the application never became
/// searchable, and a renamed vendor kept its old name in the index indefinitely.
/// </para>
/// <para>
/// Runs after the save, not before: an inserted row has no id until the database assigns one,
/// and indexing something whose transaction then rolls back would leave a phantom hit.
/// </para>
/// </summary>
public sealed class SearchIndexInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// What each in-flight save touched. Keyed by context because one interceptor instance is
    /// shared by every context, and two requests saving at once must not see each other's work.
    /// </summary>
    private readonly ConcurrentDictionary<DbContext, List<Change>> _pending = new();

    /// <summary>
    /// Holds the entity itself, not its id.
    /// <para>
    /// Capture necessarily runs before the save, and an inserted row's key is still 0 at that
    /// point — the database assigns it during the insert. Recording the id there and looking
    /// it up afterwards silently matched nothing and indexed none of the new rows. Keeping
    /// the reference means the id is read after the save, once EF has populated it.
    /// </para>
    /// </summary>
    private readonly record struct Change(object Entity, bool Removed);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        await ApplyAsync(eventData.Context, ct).ConfigureAwait(false);
        return await base.SavedChangesAsync(eventData, result, ct).ConfigureAwait(false);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ApplyAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    /// <summary>Drops captured work when the save fails, so it cannot leak into the next one.</summary>
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is not null) _pending.TryRemove(eventData.Context, out _);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken ct = default)
    {
        if (eventData.Context is not null) _pending.TryRemove(eventData.Context, out _);
        return base.SaveChangesFailedAsync(eventData, ct);
    }

    private void Capture(DbContext? context)
    {
        if (context is null) return;

        var changes = new List<Change>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            // A soft delete arrives as a Modified row with IS_DELETED set, because the audit
            // interceptor rewrites deletes. Both forms must leave the index.
            var removed = entry.State == EntityState.Deleted
                || (entry.Entity is Alkanzi.Auditable.IAuditable a && a.IS_DELETED == true);

            if (entry.Entity is Vendor or PurchaseOrder)
                changes.Add(new Change(entry.Entity, removed));
        }

        if (changes.Count > 0) _pending[context] = changes;
    }

    private async Task ApplyAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null || !_pending.TryRemove(context, out var changes)) return;
        if (context is not ErpDbContext db) return;

        foreach (var change in changes)
        {
            // Ids are read here, after the save, so inserted rows carry the key the database
            // assigned them.
            var document = await ProjectAsync(db, change.Entity, ct).ConfigureAwait(false);
            if (document is null) continue;

            var entityType = document.EntityType;
            var entityId = document.EntityId;

            var existing = await db.SearchDocuments
                .FirstOrDefaultAsync(d => d.EntityType == entityType && d.EntityId == entityId, ct)
                .ConfigureAwait(false);

            if (change.Removed)
            {
                if (existing is not null) db.SearchDocuments.Remove(existing);
                continue;
            }

            if (existing is null)
            {
                db.SearchDocuments.Add(document);
            }
            else
            {
                // Updated in place rather than delete-and-insert so the row keeps its id, and
                // so a concurrent reader never sees a moment with no row for this document.
                existing.CompanyId = document.CompanyId;
                existing.Label = document.Label;
                existing.DocNum = document.DocNum;
                existing.Title = document.Title;
                existing.Subtitle = document.Subtitle;
                existing.Keywords = document.Keywords;
                existing.BranchId = document.BranchId;
                existing.DocDate = document.DocDate;
            }
        }

        // Safe from recursion: this save touches only SearchDocument, which Capture ignores,
        // so the second pass records nothing and the chain stops.
        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Projects the tracked entity straight to a document. No re-query for the entity itself —
    /// it is already in memory and now carries its assigned id. A purchase order still needs
    /// its vendor's name, which is looked up only when the navigation was not loaded.
    /// </summary>
    private static async Task<SearchDocument?> ProjectAsync(ErpDbContext db, object entity, CancellationToken ct)
    {
        switch (entity)
        {
            case Vendor vendor:
                return SearchIndexer.ToDocument(vendor);

            case PurchaseOrder order:
                var vendorName = order.Vendor?.Name
                    ?? await db.Vendors
                        .Where(v => v.Id == order.VendorId)
                        .Select(v => v.Name)
                        .FirstOrDefaultAsync(ct)
                        .ConfigureAwait(false);
                return SearchIndexer.ToDocument(order, vendorName);

            default:
                return null;
        }
    }
}
