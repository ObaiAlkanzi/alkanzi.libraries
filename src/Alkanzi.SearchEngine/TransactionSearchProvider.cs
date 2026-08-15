using Microsoft.EntityFrameworkCore;

namespace Alkanzi.SearchEngine;

/// <summary>
/// A ready-made provider for any <see cref="ISearchableTransaction"/> entity. It matches
/// a numeric term against the document id (and document number), applies the optional
/// date window, honours soft-delete and branch scope, and maps to <see cref="SearchHit"/>s.
/// <para>
/// Name/text matching is intentionally left to entity-specific providers (each entity
/// stores its "party" name differently), so this base stays generic and index-friendly.
/// Compose or subclass to add those.
/// </para>
/// </summary>
/// <example>
/// <code>
/// services.AddSearchProvider(sp =>
///     new TransactionSearchProvider&lt;IM_PURCHASE_ORDERS&gt;(
///         "inventory",
///         () => sp.GetRequiredService&lt;FakhruddinAppDbContext&gt;().IM_PURCHASE_ORDERS,
///         title: x => $"LPO-{x.ID}"));
/// </code>
/// </example>
public class TransactionSearchProvider<T> : ISearchProvider where T : class, ISearchableTransaction
{
    private readonly Func<IQueryable<T>> _source;
    private readonly Func<T, string>? _title;
    private readonly Func<T, string?>? _subtitle;

    public TransactionSearchProvider(
        string entityType,
        Func<IQueryable<T>> source,
        Func<T, string>? title = null,
        Func<T, string?>? subtitle = null)
    {
        EntityType = string.IsNullOrWhiteSpace(entityType)
            ? throw new ArgumentException("entityType is required", nameof(entityType))
            : entityType;
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _title = title;
        _subtitle = subtitle;
    }

    public string EntityType { get; }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, SearchScope scope, CancellationToken ct = default)
    {
        // Id / document-number search only — names are the entity providers' job.
        if (query.NumericValue is not int n)
            return Array.Empty<SearchHit>();

        var q = _source().Where(x => !x.IS_DELETED && (x.ID == n || x.DOC_NUM == n));

        if (query.DateFrom is DateTime from) q = q.Where(x => x.DOC_DATE >= from);
        if (query.DateTo is DateTime to) q = q.Where(x => x.DOC_DATE <= to);
        if (scope.AllowedBranches is { Count: > 0 } br) q = q.Where(x => br.Contains(x.BRANCH_ID));

        var take = query.PerProviderLimit <= 0 ? 25 : query.PerProviderLimit;
        var rows = await q.Take(take).ToListAsync(ct).ConfigureAwait(false);

        return rows.Select(x => new SearchHit
        {
            EntityType = EntityType,
            Id = x.ID,
            Title = _title?.Invoke(x) ?? $"{EntityType} {x.ID}",
            Subtitle = _subtitle?.Invoke(x),
            BranchId = x.BRANCH_ID,
            // Exact-id beats a document-number match.
            Score = x.ID == n ? 2.0 : 1.0,
        }).ToList();
    }
}
