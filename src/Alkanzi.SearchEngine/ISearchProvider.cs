namespace Alkanzi.SearchEngine;

/// <summary>
/// Searches one entity family and returns uniform <see cref="SearchHit"/>s. Register
/// one per module; the <see cref="ISearchEngine"/> fans out across all of them.
/// </summary>
public interface ISearchProvider
{
    /// <summary>Stable key for this family, e.g. "inventory", "call", "vendor".</summary>
    string EntityType { get; }

    /// <summary>Find matches for <paramref name="query"/> that <paramref name="scope"/> permits.</summary>
    Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, SearchScope scope, CancellationToken ct = default);
}
