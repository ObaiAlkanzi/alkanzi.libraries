using Alkanzi.Application.Dtos;

namespace Alkanzi.Application.Abstractions;

/// <summary>Use cases behind the procurement workspace (KPIs + explorer).</summary>
public interface IProcurementService
{
    Task<IReadOnlyList<KpiTileDto>> GetKpisAsync(CancellationToken ct = default);

    Task<ExplorerPageDto> GetExplorerAsync(string tab, string? term, int skip, int take, CancellationToken ct = default);

    Task<IReadOnlyList<VendorOrderStatDto>> GetTopVendorsAsync(int top = 10, CancellationToken ct = default);
}
