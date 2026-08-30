using Alkanzi.Erp.DataAccess.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkanzi.Erp.Api.Controllers;

/// <summary>
/// Omni-search over the unified index. Company scoping happens inside
/// <see cref="SearchService"/>, from the caller's token, not from anything on the query string.
/// </summary>
[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly SearchService _search;

    public SearchController(SearchService search) => _search = search;

    [HttpGet]
    public async Task<IActionResult> Get(
        string? term, string? type, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var result = await _search.SearchAsync(term, type, skip, take, ct);

        return Ok(new
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
}
