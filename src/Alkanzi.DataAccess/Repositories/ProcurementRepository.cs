using Alkanzi.Application.Abstractions;
using Alkanzi.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.DataAccess.Repositories;

/// <summary>EF Core implementation of the procurement data port. Maps entities to DTOs.</summary>
public sealed class ProcurementRepository : IProcurementRepository
{
    private readonly AppDbContext _db;

    public ProcurementRepository(AppDbContext db) => _db = db;

    public async Task<ProcurementCounts> GetCountsAsync(CancellationToken ct = default)
    {
        var lpos = _db.IM_PURCHASE_ORDERS.Where(x => !x.IS_DELETED);
        var calls = _db.CALL_REGISTERATION.Where(x => !x.IS_DELETED);
        var vendors = _db.FM_SUPPLIER_MASTER.Where(x => !x.IS_DELETED);

        return new ProcurementCounts(
            PurchaseOrders: await lpos.CountAsync(ct).ConfigureAwait(false),
            PendingApproval: await lpos.CountAsync(x => x.APPROVE_STATUS == 0, ct).ConfigureAwait(false),
            Calls: await calls.CountAsync(ct).ConfigureAwait(false),
            Vendors: await vendors.CountAsync(ct).ConfigureAwait(false));
    }

    public async Task<IReadOnlyList<VendorOrderStatDto>> GetTopVendorsAsync(int top, CancellationToken ct = default)
    {
        // Project to an anonymous type first — EF can't ORDER BY a positional-record property.
        var rows = await _db.IM_PURCHASE_ORDERS
            .Where(x => !x.IS_DELETED && x.DOC_TYPE == "imPurchaseOrder" && x.ACCOUNT_NAME != null)
            .GroupBy(x => x.ACCOUNT_NAME!)
            .Select(g => new { Vendor = g.Key, Orders = g.Count() })
            .OrderByDescending(x => x.Orders)
            .Take(top)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.Select(x => new VendorOrderStatDto(x.Vendor, x.Orders)).ToList();
    }

    public async Task<ExplorerPageDto> GetExplorerAsync(string tab, string? term, int skip, int take, CancellationToken ct = default)
    {
        term = (term ?? "").Trim();
        int.TryParse(term, out var num);
        var t = term.ToUpper();

        switch (tab)
        {
            case "call":
            {
                var q = _db.CALL_REGISTERATION.Where(x => !x.IS_DELETED);
                if (term.Length > 0)
                    q = q.Where(x => x.DOC_NUM == num || (x.NAME != null && x.NAME.ToUpper().Contains(t)));
                var total = await q.CountAsync(ct).ConfigureAwait(false);
                var rows = await q.OrderByDescending(x => x.ID).Skip(skip).Take(take)
                    .Select(x => new ExplorerRowDto(x.ID, x.DOC_NUM, x.NAME, x.DOC_DATE, x.BRANCH_ID))
                    .ToListAsync(ct).ConfigureAwait(false);
                return new ExplorerPageDto("call", total, rows);
            }
            case "vendor":
            {
                var q = _db.FM_SUPPLIER_MASTER.Where(x => !x.IS_DELETED);
                if (term.Length > 0)
                    q = q.Where(x => x.ID == num || (x.NAME != null && x.NAME.ToUpper().Contains(t)));
                var total = await q.CountAsync(ct).ConfigureAwait(false);
                var rows = await q.OrderBy(x => x.NAME).Skip(skip).Take(take)
                    .Select(x => new ExplorerRowDto(x.ID, null, x.NAME, null, x.BRANCH_ID))
                    .ToListAsync(ct).ConfigureAwait(false);
                return new ExplorerPageDto("vendor", total, rows);
            }
            default: // lpo
            {
                var q = _db.IM_PURCHASE_ORDERS.Where(x => !x.IS_DELETED && x.DOC_TYPE == "imPurchaseOrder");
                if (term.Length > 0)
                    q = q.Where(x => x.DOC_NUM == num || (x.ACCOUNT_NAME != null && x.ACCOUNT_NAME.ToUpper().Contains(t)));
                var total = await q.CountAsync(ct).ConfigureAwait(false);
                var rows = await q.OrderByDescending(x => x.ID).Skip(skip).Take(take)
                    .Select(x => new ExplorerRowDto(x.ID, x.DOC_NUM, x.ACCOUNT_NAME, x.DOC_DATE, x.BRANCH_ID))
                    .ToListAsync(ct).ConfigureAwait(false);
                return new ExplorerPageDto("lpo", total, rows);
            }
        }
    }
}
