using Alkanzi.Erp.DataAccess;
using Alkanzi.Erp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.Api.Controllers;

public sealed record BranchInput(int CompanyId, string Code, string Name, bool IsActive);

/// <summary>The branch level of the IT workspace tree — the leaves.</summary>
[ApiController]
[Route("api/branches")]
[Authorize(Roles = "Super Admin")]
public class BranchesController : ControllerBase
{
    private readonly ErpDbContext _db;

    public BranchesController(ErpDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int companyId, CancellationToken ct)
    {
        var rows = await _db.Branches.AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Name)
            .Select(b => new
            {
                id = b.Id, companyId = b.CompanyId, code = b.Code,
                name = b.Name, isActive = b.IsActive,
            })
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var row = await _db.Branches.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new { id = b.Id, companyId = b.CompanyId, code = b.Code, name = b.Name, isActive = b.IsActive })
            .FirstOrDefaultAsync(ct);

        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BranchInput input, CancellationToken ct)
    {
        var error = await ValidateAsync(input, null, ct);
        if (error is not null) return error;

        var branch = new Branch
        {
            CompanyId = input.CompanyId,
            Code = input.Code.Trim(),
            Name = input.Name.Trim(),
            IsActive = input.IsActive,
        };

        _db.Branches.Add(branch);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = branch.Id }, new
        {
            id = branch.Id, companyId = branch.CompanyId, code = branch.Code,
            name = branch.Name, isActive = branch.IsActive,
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BranchInput input, CancellationToken ct)
    {
        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (branch is null) return NotFound();

        var error = await ValidateAsync(input, id, ct);
        if (error is not null) return error;

        branch.CompanyId = input.CompanyId;
        branch.Code = input.Code.Trim();
        branch.Name = input.Name.Trim();
        branch.IsActive = input.IsActive;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (branch is null) return NotFound();

        // Documents carry a branch id with a restricted foreign key, so a hard delete would
        // fail at the database. Refusing here turns that into an explanation.
        if (await _db.PurchaseOrders.AnyAsync(p => p.BranchId == id, ct))
            return Conflict(new { error = "in_use", message = "This branch is referenced by purchase orders and cannot be deleted." });

        if (await _db.Users.AnyAsync(u => u.BranchId == id, ct))
            return Conflict(new { error = "in_use", message = "This branch is a default for one or more users." });

        _db.Branches.Remove(branch);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<IActionResult?> ValidateAsync(BranchInput input, int? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
            return BadRequest(new { error = "validation", message = "Code and name are required." });

        if (!await _db.Companies.AnyAsync(c => c.Id == input.CompanyId, ct))
            return BadRequest(new { error = "validation", message = "That company does not exist." });

        if (await _db.Branches.AnyAsync(b => b.CompanyId == input.CompanyId && b.Code == input.Code && (id == null || b.Id != id), ct))
            return Conflict(new { error = "duplicate_code", message = $"Branch code '{input.Code}' is already used in this company." });

        return null;
    }
}
