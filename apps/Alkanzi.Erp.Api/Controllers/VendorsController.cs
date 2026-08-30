using Alkanzi.Erp.Application.Abstractions;
using Alkanzi.Erp.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.Api.Controllers;

[ApiController]
[Route("api/vendors")]
[Authorize]
public class VendorsController : ControllerBase
{
    private readonly ErpDbContext _db;
    private readonly ICurrentUser _currentUser;

    public VendorsController(ErpDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Paged vendor list for the caller's company.</summary>
    [HttpGet]
    public async Task<IActionResult> List(string? term, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        // Every query is scoped to the caller's company. Doing it here rather than trusting a
        // client-supplied companyId is the whole point: a parameter can be edited, a claim
        // signed into the token cannot.
        var q = _db.Vendors.AsNoTracking().Where(v => v.CompanyId == _currentUser.CompanyId);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term.Trim()}%";
            q = q.Where(v => EF.Functions.ILike(v.Name, pattern));
        }

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderBy(v => v.Name)
            .Skip(Math.Max(0, skip))
            .Take(take is <= 0 or > 200 ? 50 : take)
            .Select(v => new
            {
                id = v.Id,
                name = v.Name,
                contactPerson = v.ContactPerson,
                email = v.Email,
                phone = v.Phone,
                trn = v.Trn,
                branchId = v.BranchId,
            })
            .ToListAsync(ct);

        return Ok(new { total, rows });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var vendor = await _db.Vendors.AsNoTracking()
            .Where(v => v.Id == id && v.CompanyId == _currentUser.CompanyId)
            .Select(v => new
            {
                id = v.Id,
                name = v.Name,
                contactPerson = v.ContactPerson,
                email = v.Email,
                phone = v.Phone,
                trn = v.Trn,
                branchId = v.BranchId,
                orderCount = v.PurchaseOrders.Count(),
            })
            .FirstOrDefaultAsync(ct);

        // 404 rather than 403 for a vendor in another company: confirming that an id exists
        // but belongs to someone else is itself a disclosure.
        return vendor is null ? NotFound() : Ok(vendor);
    }
}
