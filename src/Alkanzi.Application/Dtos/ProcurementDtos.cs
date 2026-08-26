namespace Alkanzi.Application.Dtos;

/// <summary>Headline counts for the workspace KPI strip (raw data from the repository).</summary>
public sealed record ProcurementCounts(int PurchaseOrders, int PendingApproval, int Calls, int Vendors);

/// <summary>A KPI tile, composed by the application layer (label/icon/tone are use-case concerns).</summary>
public sealed record KpiTileDto(string Key, string Label, int Value, string Icon, string Tone);

/// <summary>A single explorer row (LPO, call or vendor).</summary>
public sealed record ExplorerRowDto(int Id, int? DocNum, string? Title, DateTime? Date, int BranchId);

/// <summary>A page of explorer rows for one tab.</summary>
public sealed record ExplorerPageDto(string Tab, int Total, IReadOnlyList<ExplorerRowDto> Rows);

/// <summary>A vendor ranked by number of purchase orders (for the Top Vendors chart).</summary>
public sealed record VendorOrderStatDto(string Vendor, int Orders);
