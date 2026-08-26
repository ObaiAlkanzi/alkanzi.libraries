using Alkanzi.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Alkanzi.Api.Controllers;

/// <summary>Omni-search endpoint. Pure delivery — delegates to the application layer.</summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _search;

    public SearchController(ISearchService search) => _search = search;

    /// <summary>GET api/search?term=acme&amp;types=vendor,inventory&amp;skip=0&amp;take=25</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? term,
        [FromQuery] string? types,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25,
        CancellationToken ct = default)
    {
        var result = await _search.SearchAsync(term, types, skip, take, ct);
        return Ok(new
        {
            total = result.Total,
            hits = result.Hits.Select(h => new
            {
                entityType = h.EntityType,
                id = h.Id,
                title = h.Title,
                subtitle = h.Subtitle,
                branchId = h.BranchId,
                score = h.Score,
            }),
        });
    }
}
