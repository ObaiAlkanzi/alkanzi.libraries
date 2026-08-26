using Alkanzi.Application.Dtos;

namespace Alkanzi.Application.Abstractions;

/// <summary>
/// Data port for procurement reads. Implemented by the infrastructure layer (EF Core).
/// Returns DTOs only — persistence types never cross this boundary.
/// </summary>
public interface IProcurementRepository
{
    Task<ProcurementCounts> GetCountsAsync(CancellationToken ct = default);

    Task<ExplorerPageDto> GetExplorerAsync(string tab, string? term, int skip, int take, CancellationToken ct = default);

    Task<IReadOnlyList<VendorOrderStatDto>> GetTopVendorsAsync(int top, CancellationToken ct = default);
}
