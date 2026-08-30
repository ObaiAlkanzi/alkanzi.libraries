using Alkanzi.Erp.DataAccess;
using Alkanzi.Erp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.Api.Controllers;

public sealed record OrganizationInput(string Code, string Name, bool IsActive);

/// <summary>
/// The organization level of the IT workspace tree.
/// <para>
/// Restricted to Super Admin: this is the structure every other record hangs off, and a
/// mistake here is not a data-entry error but a change to who can see what.
/// </para>
/// </summary>
[ApiController]
[Route("api/organizations")]
[Authorize(Roles = "Super Admin")]
public class OrganizationsController : ControllerBase
{
    private readonly ErpDbContext _db;

    public OrganizationsController(ErpDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var rows = await _db.Organizations.AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new
            {
                id = o.Id,
                code = o.Code,
                name = o.Name,
                isActive = o.IsActive,
                companyCount = o.Companies.Count(),
            })
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var row = await _db.Organizations.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new { id = o.Id, code = o.Code, name = o.Name, isActive = o.IsActive })
            .FirstOrDefaultAsync(ct);

        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrganizationInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
            return BadRequest(new { error = "validation", message = "Code and name are required." });

        // Checked before insert so the caller gets a readable message rather than a raw unique
        // violation. The partial unique index is still what actually guarantees it.
        if (await _db.Organizations.AnyAsync(o => o.Code == input.Code, ct))
            return Conflict(new { error = "duplicate_code", message = $"Organization code '{input.Code}' is already in use." });

        var organization = new Organization
        {
            Code = input.Code.Trim(),
            Name = input.Name.Trim(),
            IsActive = input.IsActive,
        };

        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = organization.Id },
            new { id = organization.Id, code = organization.Code, name = organization.Name, isActive = organization.IsActive });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] OrganizationInput input, CancellationToken ct)
    {
        var organization = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (organization is null) return NotFound();

        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
            return BadRequest(new { error = "validation", message = "Code and name are required." });

        if (await _db.Organizations.AnyAsync(o => o.Code == input.Code && o.Id != id, ct))
            return Conflict(new { error = "duplicate_code", message = $"Organization code '{input.Code}' is already in use." });

        organization.Code = input.Code.Trim();
        organization.Name = input.Name.Trim();
        organization.IsActive = input.IsActive;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes the organization. The audit interceptor rewrites the delete, so the row
    /// survives and stops being visible.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var organization = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (organization is null) return NotFound();

        // Refused rather than cascaded. Soft-deleting an organization hides its companies'
        // users too — the query filter on ApplicationUser follows the company — so this would
        // silently lock people out. Deleting the children first makes that a deliberate act.
        if (await _db.Companies.AnyAsync(c => c.OrganizationId == id, ct))
        {
            return Conflict(new
            {
                error = "has_children",
                message = "This organization still has companies. Delete or move them first.",
            });
        }

        _db.Organizations.Remove(organization);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
