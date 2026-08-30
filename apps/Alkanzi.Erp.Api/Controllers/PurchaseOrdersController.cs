using Alkanzi.Erp.Application.Abstractions;
using Alkanzi.Erp.DataAccess;
using Alkanzi.Erp.Domain.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly ErpDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PurchaseOrdersController(ErpDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        ApprovalStatus? status, int? branchId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var q = _db.PurchaseOrders.AsNoTracking()
            .Where(p => p.CompanyId == _currentUser.CompanyId);

        if (status is ApprovalStatus s) q = q.Where(p => p.Status == s);
        if (branchId is int b) q = q.Where(p => p.BranchId == b);

        var total = await q.CountAsync(ct);

        var rows = await q
            // Document number as the final tiebreak, so paging is a stable slice: two orders
            // sharing a date could otherwise swap places between pages and one would repeat.
            .OrderByDescending(p => p.DocDate).ThenByDescending(p => p.DocNum)
            .Skip(Math.Max(0, skip))
            .Take(take is <= 0 or > 200 ? 50 : take)
            .Select(p => new
            {
                id = p.Id,
                docNum = p.DocNum,
                docDate = p.DocDate,
                vendorId = p.VendorId,
                vendor = p.Vendor!.Name,
                amount = p.Amount,
                currency = p.Currency,
                status = p.Status.ToString(),
                branchId = p.BranchId,
                remarks = p.Remarks,
            })
            .ToListAsync(ct);

        return Ok(new { total, rows });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders.AsNoTracking()
            .Where(p => p.Id == id && p.CompanyId == _currentUser.CompanyId)
            .Select(p => new
            {
                id = p.Id,
                docNum = p.DocNum,
                docDate = p.DocDate,
                vendorId = p.VendorId,
                vendor = p.Vendor!.Name,
                amount = p.Amount,
                currency = p.Currency,
                status = p.Status.ToString(),
                branchId = p.BranchId,
                remarks = p.Remarks,
            })
            .FirstOrDefaultAsync(ct);

        return order is null ? NotFound() : Ok(order);
    }
}
