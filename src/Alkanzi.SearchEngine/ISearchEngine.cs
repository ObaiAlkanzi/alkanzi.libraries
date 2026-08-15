namespace Alkanzi.SearchEngine;

/// <summary>The one entry point callers use: fans out to providers, merges, ranks, pages.</summary>
public interface ISearchEngine
{
    Task<SearchResult> SearchAsync(SearchQuery query, SearchScope scope, CancellationToken ct = default);
}
