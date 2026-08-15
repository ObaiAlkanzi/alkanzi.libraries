namespace Alkanzi.SearchEngine;

/// <summary>
/// A provider built from a delegate — the quickest way to add a search source without
/// writing a class. Useful for one-off or lightweight providers (e.g. a raw SQL / Oracle
/// Text query) registered inline.
/// </summary>
/// <example>
/// <code>
/// services.AddSearchProvider("vendor", (q, scope, ct) =>
///     vendorRepo.SearchAsync(q.Term, q.PerProviderLimit, ct));
/// </code>
/// </example>
public sealed class DelegateSearchProvider : ISearchProvider
{
    private readonly Func<SearchQuery, SearchScope, CancellationToken, Task<IReadOnlyList<SearchHit>>> _search;

    public DelegateSearchProvider(
        string entityType,
        Func<SearchQuery, SearchScope, CancellationToken, Task<IReadOnlyList<SearchHit>>> search)
    {
        EntityType = string.IsNullOrWhiteSpace(entityType)
            ? throw new ArgumentException("entityType is required", nameof(entityType))
            : entityType;
        _search = search ?? throw new ArgumentNullException(nameof(search));
    }

    public string EntityType { get; }

    public Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, SearchScope scope, CancellationToken ct = default)
        => _search(query, scope, ct);
}
