using Alkanzi.SearchEngine;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.DataAccess.Search;

/// <summary>
/// The unified provider: one query over <c>SEARCH_INDEX</c> that serves every entity type at
/// once. Declares the wildcard type "*", so the engine always runs it and this provider applies
/// the type/branch/scope filters itself. Swap this for an Elasticsearch-backed provider later
/// without touching the engine, services or UI.
/// </summary>
public sealed class SearchIndexProvider : ISearchProvider
{
    private readonly AppDbContext _db;

    public SearchIndexProvider(AppDbContext db) => _db = db;

    public string EntityType => "*";

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, SearchScope scope, CancellationToken ct = default)
    {
        var q = _db.SearchIndex.AsNoTracking();

        // ---- type scoping (permissions + explicit filter) ----
        if (scope.AllowedTypes is { Count: > 0 } allowed)
            q = q.Where(x => allowed.Contains(x.EntityType));
        if (query.Types is { Count: > 0 } types)
            q = q.Where(x => types.Contains(x.EntityType));

        // ---- branch scoping (0 = global, always visible) ----
        if (scope.AllowedBranches is { Count: > 0 } branches)
            q = q.Where(x => x.BranchId == 0 || branches.Contains(x.BranchId));

        // ---- term match ----
        int? n = query.NumericValue;
        var t = (query.Term ?? "").Trim().ToUpper();

        if (n is int id)
        {
            q = q.Where(x => x.EntityId == id || x.DocNum == id || x.Keywords.Contains(t));
            q = q.OrderByDescending(x => x.EntityId == id || x.DocNum == id) // exact id/doc first
                 .ThenByDescending(x => x.DocDate)
                 .ThenByDescending(x => x.EntityId);
        }
        else
        {
            if (t.Length < 2) return Array.Empty<SearchHit>();
            q = q.Where(x => x.Keywords.Contains(t));
            q = q.OrderByDescending(x => x.DocDate).ThenByDescending(x => x.EntityId);
        }

        if (query.DateFrom is DateTime from) q = q.Where(x => x.DocDate >= from);
        if (query.DateTo is DateTime to) q = q.Where(x => x.DocDate <= to);

        var take = query.PerProviderLimit <= 0 ? 25 : query.PerProviderLimit;
        var rows = await q.Take(take).ToListAsync(ct).ConfigureAwait(false);

        return rows.Select(x => new SearchHit
        {
            EntityType = x.EntityType,
            Id = x.EntityId,
            Title = x.Title,
            Subtitle = x.Subtitle,
            BranchId = x.BranchId,
            Score = (n is int nn && (x.EntityId == nn || x.DocNum == nn)) ? 2.0 : 1.0,
        }).ToList();
    }
}
