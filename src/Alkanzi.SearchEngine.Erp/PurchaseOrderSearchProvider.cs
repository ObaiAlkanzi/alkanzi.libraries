using Alkanzi.SearchEngine;
using Microsoft.EntityFrameworkCore;
using Modules_DataTables.IM_MODULES;

namespace Alkanzi.SearchEngine.Erp;

/// <summary>
/// Searches inventory LPOs (<see cref="IM_PURCHASE_ORDERS"/>). A numeric term matches the
/// id or document number; any other term matches the supplier name. Honours soft-delete,
/// the date window and branch scope. Supplier name is the subtitle.
/// </summary>
public sealed class PurchaseOrderSearchProvider : ISearchProvider
{
    private readonly Func<IQueryable<IM_PURCHASE_ORDERS>> _source;

    /// <param name="source">
    /// Yields the queryable set, e.g. <c>() =&gt; db.IM_PURCHASE_ORDERS</c>.
    /// </param>
    public PurchaseOrderSearchProvider(Func<IQueryable<IM_PURCHASE_ORDERS>> source)
        => _source = source ?? throw new ArgumentNullException(nameof(source));

    public string EntityType => "inventory";

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, SearchScope scope, CancellationToken ct = default)
    {
        var q = _source().Where(x => !x.IS_DELETED && x.DOC_TYPE == "imPurchaseOrder");

        int? n = query.NumericValue;
        if (n is int id)
        {
            q = q.Where(x => x.ID == id || x.DOC_NUM == id);
        }
        else
        {
            var t = query.Term.Trim().ToUpper();
            if (t.Length < 2) return Array.Empty<SearchHit>();
            q = q.Where(x => x.ACCOUNT_NAME != null && x.ACCOUNT_NAME.ToUpper().Contains(t));
        }

        if (query.DateFrom is DateTime from) q = q.Where(x => x.DOC_DATE >= from);
        if (query.DateTo is DateTime to) q = q.Where(x => x.DOC_DATE <= to);
        if (scope.AllowedBranches is { Count: > 0 } br) q = q.Where(x => br.Contains(x.BRANCH_ID));

        var take = query.PerProviderLimit <= 0 ? 25 : query.PerProviderLimit;
        var rows = await q.Take(take).ToListAsync(ct).ConfigureAwait(false);

        return rows.Select(x => new SearchHit
        {
            EntityType = EntityType,
            Id = x.ID,
            Title = $"LPO-{x.ID}",
            Subtitle = x.ACCOUNT_NAME,
            BranchId = x.BRANCH_ID,
            Score = (n is int nn && x.ID == nn) ? 2.0 : 1.0,
        }).ToList();
    }
}
