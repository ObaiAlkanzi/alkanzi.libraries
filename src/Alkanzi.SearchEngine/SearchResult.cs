namespace Alkanzi.SearchEngine;

/// <summary>The merged, ranked, paged outcome of a search.</summary>
public sealed class SearchResult
{
    /// <summary>The current page of hits, best-ranked first.</summary>
    public IReadOnlyList<SearchHit> Hits { get; init; } = Array.Empty<SearchHit>();

    /// <summary>Total hits across all providers before paging.</summary>
    public int Total { get; init; }

    public static SearchResult Empty { get; } = new();
}
