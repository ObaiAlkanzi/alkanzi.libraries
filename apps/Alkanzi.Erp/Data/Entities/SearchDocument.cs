using NpgsqlTypes;

namespace Alkanzi.Erp.Data.Entities;

/// <summary>
/// One row of the unified search index: every searchable record from any table projected into
/// a single uniform shape, so one query serves an omni-search box across the whole ERP.
/// <para>
/// The point of this design on PostgreSQL is <see cref="SearchVector"/>. It is a
/// <c>GENERATED ALWAYS AS ... STORED</c> column, so the database maintains it inside the same
/// transaction as the write. There is no interceptor to run, no outbox to drain and no
/// reindex job — the index physically cannot drift from the row it describes, which is the
/// expensive failure mode of keeping search in a separate datastore.
/// </para>
/// <para>
/// When this eventually outgrows PostgreSQL full-text search — typo tolerance as a product
/// feature, faceted counts over tens of millions of rows, relevance tuning without a deploy —
/// this table is also the natural thing to ship to a dedicated engine, because it already has
/// the uniform shape such an engine wants.
/// </para>
/// </summary>
public class SearchDocument
{
    public int Id { get; set; }

    /// <summary>Provider/entity key: "vendor", "purchase_order", …</summary>
    public string EntityType { get; set; } = "";

    /// <summary>Human label for the type — "Purchase Order", "Vendor" — indexed rather than
    /// derived in the client, so a newly indexed type is labelled without a front-end change.</summary>
    public string Label { get; set; } = "";

    /// <summary>The source record's id.</summary>
    public long EntityId { get; set; }

    /// <summary>Document number where the source has one, for id-style lookups.</summary>
    public int? DocNum { get; set; }

    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }

    /// <summary>Everything else worth matching on, flattened into one blob.</summary>
    public string? Keywords { get; set; }

    public int BranchId { get; set; }
    public DateOnly? DocDate { get; set; }

    /// <summary>
    /// Maintained by PostgreSQL from Title/Subtitle/Keywords — never assigned in code.
    /// Weighted so a title match outranks a keyword match.
    /// </summary>
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}
