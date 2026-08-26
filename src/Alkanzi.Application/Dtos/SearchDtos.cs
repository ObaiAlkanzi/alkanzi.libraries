namespace Alkanzi.Application.Dtos;

/// <summary>A single search result, shaped for delivery (no engine/EF types leak out).</summary>
public sealed record SearchHitDto(
    string EntityType,
    long Id,
    string Title,
    string? Subtitle,
    int BranchId,
    double Score);

/// <summary>A page of merged, ranked search hits.</summary>
public sealed record SearchResultDto(int Total, IReadOnlyList<SearchHitDto> Hits)
{
    public static readonly SearchResultDto Empty = new(0, Array.Empty<SearchHitDto>());
}
