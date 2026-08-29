using System.Diagnostics;
using Alkanzi.Erp.Data;
using Alkanzi.Erp.Data.Entities;
using Alkanzi.Erp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.Controllers;

public class HomeController : Controller
{
    private readonly ErpDbContext _db;
    private readonly SearchService _search;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ErpDbContext db, SearchService search, ILogger<HomeController> logger)
    {
        _db = db;
        _search = search;
        _logger = logger;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }

    /// <summary>Dashboard feed: KPI counts, value by vendor, and the most recent orders.</summary>
    [HttpGet]
    public async Task<IActionResult> DashboardData(CancellationToken ct)
    {
        // Aggregated in the database rather than by materialising every order and summing in
        // memory — this stays a handful of rows over the wire no matter how the table grows.
        var counts = await _db.PurchaseOrders
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Orders = g.Count(),
                Pending = g.Count(x => x.Status == ApprovalStatus.Pending),
                Value = g.Sum(x => (decimal?)x.Amount) ?? 0m,
            })
            .FirstOrDefaultAsync(ct)
            ?? new { Orders = 0, Pending = 0, Value = 0m };

        var vendorCount = await _db.Vendors.CountAsync(ct);

        var byVendor = await _db.PurchaseOrders
            .GroupBy(x => x.Vendor!.Name)
            .Select(g => new { vendor = g.Key, amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.amount)
            .Take(10)
            .ToListAsync(ct);

        var orders = await _db.PurchaseOrders
            .OrderByDescending(x => x.DocDate).ThenByDescending(x => x.Id)
            .Take(50)
            .Select(x => new
            {
                id = x.DocNum,
                vendor = x.Vendor!.Name,
                date = x.DocDate,
                amount = x.Amount,
                status = x.Status.ToString(),
            })
            .ToListAsync(ct);

        return Json(new
        {
            kpis = new[]
            {
                new { key = "orders",  label = "Purchase Orders",   value = counts.Orders },
                new { key = "pending", label = "Pending Approval",  value = counts.Pending },
                new { key = "vendors", label = "Active Vendors",    value = vendorCount },
                new { key = "value",   label = "Total Value (AED)", value = (int)counts.Value },
            },
            orders,
            byVendor
        });
    }

    /// <summary>Omni-search over the unified index. Backs the toolbar search box.</summary>
    [HttpGet]
    public async Task<IActionResult> Search(string? term, string? type, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var result = await _search.SearchAsync(term, type, skip, take, ct);
        return Json(new
        {
            total = result.Total,
            hits = result.Hits.Select(h => new
            {
                entityType = h.EntityType,
                label = h.Label,
                id = h.Id,
                docNum = h.DocNum,
                title = h.Title,
                subtitle = h.Subtitle,
                branchId = h.BranchId,
                rank = h.Rank,
            })
        });
    }

    public IActionResult Privacy()
    {
        ViewData["Title"] = "Privacy";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
