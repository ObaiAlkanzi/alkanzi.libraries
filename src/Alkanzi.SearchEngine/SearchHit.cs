namespace Alkanzi.SearchEngine;

/// <summary>
/// One search result, uniform across every entity family so a single UI can render
/// "Type · Title · Subtitle" and route to the document.
/// </summary>
public sealed class SearchHit
{
    /// <summary>Provider key, e.g. "inventory", "call", "requisition", "vendor".</summary>
    public required string EntityType { get; init; }

    /// <summary>The document id.</summary>
    public required long Id { get; init; }

    /// <summary>Primary label, e.g. "LPO-512" or "Call 4008".</summary>
    public required string Title { get; init; }

    /// <summary>Secondary detail, e.g. "supplier · status" or "from Call 4008".</summary>
    public string? Subtitle { get; init; }

    /// <summary>Relevance score; higher sorts first. Exact-id hits should outscore fuzzy ones.</summary>
    public double Score { get; init; } = 1.0;

    /// <summary>Branch the document belongs to (used for branch-scoped permission filtering).</summary>
    public int BranchId { get; init; }

    /// <summary>Optional extra data the UI needs to open the document (route params, doc type…).</summary>
    public IReadOnlyDictionary<string, string?>? Metadata { get; init; }
}
