using Alkanzi.Erp.Application.Abstractions;
using Alkanzi.Erp.DataAccess;
using Alkanzi.Erp.Domain.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.Api.Controllers;

/// <summary>
/// The dashboard feed. Moved here from the web project's own controller: the web app is a
/// client of this API and no longer reaches the database itself.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ErpDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DashboardController(ErpDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;

        // Aggregated in the database rather than by materialising every order and summing in
        // memory — this stays a handful of rows over the wire however the table grows.
        var counts = await _db.PurchaseOrders
            .Where(p => p.CompanyId == companyId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Orders = g.Count(),
                Pending = g.Count(x => x.Status == ApprovalStatus.Pending),
                Value = g.Sum(x => (decimal?)x.Amount) ?? 0m,
            })
            .FirstOrDefaultAsync(ct)
            ?? new { Orders = 0, Pending = 0, Value = 0m };

        var vendorCount = await _db.Vendors.CountAsync(v => v.CompanyId == companyId, ct);

        var byVendor = await _db.PurchaseOrders
            .Where(p => p.CompanyId == companyId)
            .GroupBy(x => x.Vendor!.Name)
            .Select(g => new { vendor = g.Key, amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.amount)
            .Take(10)
            .ToListAsync(ct);

        var orders = await _db.PurchaseOrders
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(x => x.DocDate).ThenByDescending(x => x.DocNum)
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

        return Ok(new
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
}
