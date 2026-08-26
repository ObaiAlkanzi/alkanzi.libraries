using Alkanzi.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Alkanzi.Api.Controllers;

/// <summary>Workspace endpoints (KPIs + explorer). Pure delivery over the application layer.</summary>
[ApiController]
[Route("api/[controller]")]
public class ProcurementController : ControllerBase
{
    private readonly IProcurementService _procurement;

    public ProcurementController(IProcurementService procurement) => _procurement = procurement;

    /// <summary>GET api/procurement/kpis — headline counts for the KPI strip.</summary>
    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis(CancellationToken ct = default)
    {
        var tiles = await _procurement.GetKpisAsync(ct);
        return Ok(tiles.Select(k => new { key = k.Key, label = k.Label, value = k.Value, icon = k.Icon, tone = k.Tone }));
    }

    /// <summary>GET api/procurement/top-vendors?top=10 — vendors ranked by purchase-order count.</summary>
    [HttpGet("top-vendors")]
    public async Task<IActionResult> TopVendors([FromQuery] int top = 10, CancellationToken ct = default)
    {
        var rows = await _procurement.GetTopVendorsAsync(top, ct);
        return Ok(rows.Select(v => new { vendor = v.Vendor, orders = v.Orders }));
    }

    /// <summary>GET api/procurement/explorer?tab=lpo|call|vendor&amp;term=&amp;skip=0&amp;take=25</summary>
    [HttpGet("explorer")]
    public async Task<IActionResult> Explorer(
        [FromQuery] string tab = "lpo",
        [FromQuery] string? term = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25,
        CancellationToken ct = default)
    {
        var page = await _procurement.GetExplorerAsync(tab, term, skip, take, ct);
        return Ok(new
        {
            tab = page.Tab,
            total = page.Total,
            rows = page.Rows.Select(r => new { id = r.Id, docNum = r.DocNum, title = r.Title, date = r.Date, branchId = r.BranchId }),
        });
    }
}
