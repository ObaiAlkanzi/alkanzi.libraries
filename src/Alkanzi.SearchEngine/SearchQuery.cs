namespace Alkanzi.SearchEngine;

/// <summary>
/// A single search request. <see cref="Term"/> is what the user typed (an id, a
/// document number, or a name). The rest are optional narrowing filters + paging.
/// </summary>
public sealed class SearchQuery
{
    /// <summary>Raw user input — an id/doc-number or a name fragment.</summary>
    public string Term { get; init; } = "";

    /// <summary>Restrict to these entity types (provider keys). Null/empty = all.</summary>
    public IReadOnlyCollection<string>? Types { get; init; }

    /// <summary>Optional document-date lower bound.</summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>Optional document-date upper bound.</summary>
    public DateTime? DateTo { get; init; }

    /// <summary>Rows to skip in the merged, ranked result (paging).</summary>
    public int Skip { get; init; }

    /// <summary>Rows to return from the merged, ranked result (paging). Default 25.</summary>
    public int Take { get; init; } = 25;

    /// <summary>Max rows each provider returns before merging. Default 25.</summary>
    public int PerProviderLimit { get; init; } = 25;

    /// <summary>True when the term is a plain integer (id / document-number search).</summary>
    public bool IsNumeric => int.TryParse(Term?.Trim(), out _);

    /// <summary>The numeric value of <see cref="Term"/>, or null when it is not numeric.</summary>
    public int? NumericValue => int.TryParse(Term?.Trim(), out var n) ? n : (int?)null;
}
