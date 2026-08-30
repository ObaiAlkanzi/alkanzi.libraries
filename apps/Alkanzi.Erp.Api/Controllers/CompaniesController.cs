using Alkanzi.Erp.DataAccess;
using Alkanzi.Erp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.Api.Controllers;

public sealed record CompanyInput(int OrganizationId, string Code, string Name, string Currency, bool IsActive);

/// <summary>The company level of the IT workspace tree.</summary>
[ApiController]
[Route("api/companies")]
[Authorize(Roles = "Super Admin")]
public class CompaniesController : ControllerBase
{
    private readonly ErpDbContext _db;

    public CompaniesController(ErpDbContext db) => _db = db;

    /// <summary>Companies under one organization — the tree's second level.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int organizationId, CancellationToken ct)
    {
        var rows = await _db.Companies.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                id = c.Id,
                organizationId = c.OrganizationId,
                code = c.Code,
                name = c.Name,
                currency = c.Currency,
                isActive = c.IsActive,
                branchCount = c.Branches.Count(),
            })
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var row = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                id = c.Id, organizationId = c.OrganizationId, code = c.Code,
                name = c.Name, currency = c.Currency, isActive = c.IsActive,
            })
            .FirstOrDefaultAsync(ct);

        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompanyInput input, CancellationToken ct)
    {
        var error = await ValidateAsync(input, null, ct);
        if (error is not null) return error;

        var company = new Company
        {
            OrganizationId = input.OrganizationId,
            Code = input.Code.Trim(),
            Name = input.Name.Trim(),
            Currency = (input.Currency ?? "AED").Trim().ToUpperInvariant(),
            IsActive = input.IsActive,
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = company.Id }, new
        {
            id = company.Id, organizationId = company.OrganizationId, code = company.Code,
            name = company.Name, currency = company.Currency, isActive = company.IsActive,
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CompanyInput input, CancellationToken ct)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (company is null) return NotFound();

        var error = await ValidateAsync(input, id, ct);
        if (error is not null) return error;

        company.OrganizationId = input.OrganizationId;
        company.Code = input.Code.Trim();
        company.Name = input.Name.Trim();
        company.Currency = (input.Currency ?? "AED").Trim().ToUpperInvariant();
        company.IsActive = input.IsActive;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (company is null) return NotFound();

        if (await _db.Branches.AnyAsync(b => b.CompanyId == id, ct))
            return Conflict(new { error = "has_children", message = "This company still has branches. Delete them first." });

        // Users are filtered by their company's deleted flag, so removing a company with users
        // still attached would hide those accounts and lock those people out with no message
        // that explains it.
        if (await _db.Users.AnyAsync(u => u.CompanyId == id, ct))
            return Conflict(new { error = "has_users", message = "This company still has users. Move or remove them first." });

        _db.Companies.Remove(company);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<IActionResult?> ValidateAsync(CompanyInput input, int? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
            return BadRequest(new { error = "validation", message = "Code and name are required." });

        if (!await _db.Organizations.AnyAsync(o => o.Id == input.OrganizationId, ct))
            return BadRequest(new { error = "validation", message = "That organization does not exist." });

        // Codes are unique within an organization, not globally.
        if (await _db.Companies.AnyAsync(c => c.OrganizationId == input.OrganizationId && c.Code == input.Code && (id == null || c.Id != id), ct))
            return Conflict(new { error = "duplicate_code", message = $"Company code '{input.Code}' is already used in this organization." });

        return null;
    }
}
