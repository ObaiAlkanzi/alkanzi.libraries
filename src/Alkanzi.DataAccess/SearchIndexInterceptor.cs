using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Alkanzi.DataAccess;

/// <summary>
/// Keeps <c>SEARCH_INDEX</c> live: whenever a searchable entity is added / modified / deleted
/// through EF, the matching index row is upserted or removed automatically — no manual reindex.
/// </summary>
/// <remarks>
/// Changes are captured <b>before</b> save (so generated identity ids are available afterwards),
/// then applied <b>after</b> the save succeeds, through a separate connection. Index maintenance
/// never throws into the caller — a failed index update is logged but never breaks the real save
/// (a full <see cref="SearchIndexBuilder.Rebuild"/> is the backstop).
/// </remarks>
public sealed class SearchIndexInterceptor : SaveChangesInterceptor
{
    private sealed class Pending
    {
        public readonly List<object> Upserts = new();
        public readonly List<(string Type, long Id)> Deletes = new();
        public bool Any => Upserts.Count > 0 || Deletes.Count > 0;
    }

    // Per-context capture; the interceptor instance is shared across contexts.
    private readonly ConditionalWeakTable<DbContext, Pending> _pending = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Capture(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Apply(eventData.Context);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        Apply(eventData.Context);
        return new ValueTask<int>(result);
    }

    private void Capture(DbContext? ctx)
    {
        if (ctx is null) return;
        var pending = new Pending();
        foreach (var entry in ctx.ChangeTracker.Entries())
        {
            if (SearchDocumentMapper.Key(entry.Entity) is not { } key) continue; // not searchable
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    pending.Upserts.Add(entry.Entity); // id read after save
                    break;
                case EntityState.Deleted:
                    pending.Deletes.Add(key);          // id available now
                    break;
            }
        }
        if (pending.Any) _pending.AddOrUpdate(ctx, pending);
    }

    private void Apply(DbContext? ctx)
    {
        if (ctx is null || !_pending.TryGetValue(ctx, out var pending)) return;
        _pending.Remove(ctx);

        try
        {
            var conn = ctx.Database.GetConnectionString();
            if (string.IsNullOrEmpty(conn)) return;

            // A separate context (no interceptor) — decoupled from the caller's save, no re-entrancy.
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conn).Options;
            using var idx = new AppDbContext(options);

            foreach (var entity in pending.Upserts)
            {
                if (SearchDocumentMapper.Key(entity) is not { } key) continue;
                var (type, id) = key;
                idx.SearchIndex.Where(d => d.EntityType == type && d.EntityId == id).ExecuteDelete();
                var doc = SearchDocumentMapper.Map(entity);
                if (doc is not null) idx.SearchIndex.Add(doc); // null => soft-deleted / not indexable => just removed
            }

            foreach (var (type, id) in pending.Deletes)
                idx.SearchIndex.Where(d => d.EntityType == type && d.EntityId == id).ExecuteDelete();

            idx.SaveChanges();
        }
        catch (Exception ex)
        {
            // Never break the business save because of index maintenance.
            Debug.WriteLine($"[SearchIndexInterceptor] index update failed: {ex.Message}");
        }
    }
}
