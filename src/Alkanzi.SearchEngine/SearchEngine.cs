namespace Alkanzi.SearchEngine;

/// <summary>
/// Default engine: runs every permitted provider in parallel, merges their hits,
/// applies branch-level permission filtering, ranks by score, and pages. A single
/// provider throwing never fails the whole search — it is skipped.
/// </summary>
public sealed class SearchEngine : ISearchEngine
{
    private readonly IEnumerable<ISearchProvider> _providers;

    public SearchEngine(IEnumerable<ISearchProvider> providers)
        => _providers = providers ?? throw new ArgumentNullException(nameof(providers));

    public async Task<SearchResult> SearchAsync(SearchQuery query, SearchScope scope, CancellationToken ct = default)
    {
        if (query is null || string.IsNullOrWhiteSpace(query.Term))
            return SearchResult.Empty;
        scope ??= SearchScope.All;

        var active = _providers.Where(p => IsAllowed(p.EntityType, query, scope)).ToList();
        if (active.Count == 0) return SearchResult.Empty;

        var batches = await Task.WhenAll(active.Select(p => RunSafeAsync(p, query, scope, ct)));

        var hits = batches
            .SelectMany(b => b)
            .Where(h => IsBranchVisible(h, scope))
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.EntityType, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(h => h.Id)
            .ToList();

        var take = query.Take <= 0 ? 25 : query.Take;
        var page = hits.Skip(Math.Max(0, query.Skip)).Take(take).ToList();
        return new SearchResult { Hits = page, Total = hits.Count };
    }

    private static bool IsAllowed(string type, SearchQuery q, SearchScope s)
        // A wildcard provider ("*") backs many entity types from one source (e.g. a unified
        // index). It always runs and applies the type/scope filters itself.
        => type == "*"
        || ((q.Types is null || q.Types.Count == 0 || q.Types.Contains(type))
            && (s.AllowedTypes is null || s.AllowedTypes.Contains(type)));

    private static bool IsBranchVisible(SearchHit h, SearchScope s)
        => s.AllowedBranches is null || h.BranchId == 0 || s.AllowedBranches.Contains(h.BranchId);

    private static async Task<IReadOnlyList<SearchHit>> RunSafeAsync(
        ISearchProvider p, SearchQuery q, SearchScope s, CancellationToken ct)
    {
        try
        {
            return await p.SearchAsync(q, s, ct).ConfigureAwait(false) ?? Array.Empty<SearchHit>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // One provider failing must not sink the whole search.
            return Array.Empty<SearchHit>();
        }
    }
}
