using Alkanzi.Application.Abstractions;
using Alkanzi.Application.Dtos;
using Alkanzi.SearchEngine;

namespace Alkanzi.Application.Services;

/// <summary>
/// Omni-search use case. Builds the <see cref="SearchQuery"/>, resolves the caller's scope,
/// runs the engine and maps hits to DTOs. Controllers depend on this, not on the engine.
/// </summary>
public sealed class SearchService : ISearchService
{
    private readonly ISearchEngine _engine;

    public SearchService(ISearchEngine engine) => _engine = engine;

    public async Task<SearchResultDto> SearchAsync(string? term, string? types, int skip, int take, CancellationToken ct = default)
    {
        term = (term ?? "").Trim();
        if (term.Length == 0)
            return SearchResultDto.Empty;

        var typeList = string.IsNullOrWhiteSpace(types)
            ? null
            : types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        skip = Math.Max(0, skip);
        take = take is <= 0 or > 100 ? 25 : take;

        // Candidate pool: to rank the requested page correctly across providers, each provider
        // must contribute at least the top (skip + take) rows. Tie fetch depth to how deep the
        // caller pages — page 1 stays cheap; deep pages fetch more. Capped so a huge skip can't
        // hammer the DB.
        var pool = Math.Clamp(skip + take, 25, 1000);

        var query = new SearchQuery
        {
            Term = term,
            Types = typeList,
            Skip = skip,
            Take = take,
            PerProviderLimit = pool,
        };

        // Demo: full access. In the ERP this scope comes from the signed-in user's capabilities.
        var result = await _engine.SearchAsync(query, SearchScope.All, ct).ConfigureAwait(false);

        var hits = result.Hits
            .Select(h => new SearchHitDto(h.EntityType, h.Id, h.Title, h.Subtitle, h.BranchId, h.Score))
            .ToList();

        return new SearchResultDto(result.Total, hits);
    }
}
