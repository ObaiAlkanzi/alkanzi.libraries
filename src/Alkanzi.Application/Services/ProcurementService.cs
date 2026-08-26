using Alkanzi.Application.Abstractions;
using Alkanzi.Application.Dtos;

namespace Alkanzi.Application.Services;

/// <summary>
/// Procurement workspace use cases. Composes KPI tiles from raw counts and applies paging
/// rules to the explorer. All data comes through <see cref="IProcurementRepository"/>.
/// </summary>
public sealed class ProcurementService : IProcurementService
{
    private readonly IProcurementRepository _repo;

    public ProcurementService(IProcurementRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<KpiTileDto>> GetKpisAsync(CancellationToken ct = default)
    {
        var c = await _repo.GetCountsAsync(ct).ConfigureAwait(false);
        return new[]
        {
            new KpiTileDto("lpos", "Purchase Orders", c.PurchaseOrders, "fa-file-invoice", "primary"),
            new KpiTileDto("pending", "Pending Approval", c.PendingApproval, "fa-hourglass-half", "warning"),
            new KpiTileDto("calls", "Registered Calls", c.Calls, "fa-phone-volume", "info"),
            new KpiTileDto("vendors", "Vendors", c.Vendors, "fa-truck-field", "success"),
        };
    }

    public Task<ExplorerPageDto> GetExplorerAsync(string tab, string? term, int skip, int take, CancellationToken ct = default)
    {
        tab = string.IsNullOrWhiteSpace(tab) ? "lpo" : tab.Trim().ToLowerInvariant();
        skip = Math.Max(0, skip);
        take = take is <= 0 or > 100 ? 25 : take;
        return _repo.GetExplorerAsync(tab, term, skip, take, ct);
    }

    public Task<IReadOnlyList<VendorOrderStatDto>> GetTopVendorsAsync(int top = 10, CancellationToken ct = default)
    {
        top = top is <= 0 or > 50 ? 10 : top;
        return _repo.GetTopVendorsAsync(top, ct);
    }
}
