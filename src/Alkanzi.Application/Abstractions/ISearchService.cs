using Alkanzi.Application.Dtos;

namespace Alkanzi.Application.Abstractions;

/// <summary>Omni-search use case. Wraps the search engine and returns delivery DTOs.</summary>
public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(string? term, string? types, int skip, int take, CancellationToken ct = default);
}
