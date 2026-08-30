using NpgsqlTypes;

namespace Alkanzi.Erp.DataAccess.Search;

/// <summary>
/// One row of the unified search index: every searchable record projected into a single
/// uniform shape, so one query serves an omni-search box across the whole ERP.
/// <para>
/// This lives in data access, not the domain. It is a derived projection of domain data
/// shaped for a particular retrieval technology — nothing in the business rules refers to it,
/// and replacing PostgreSQL full-text search with a dedicated engine would rewrite this
/// without touching a single domain type.
/// </para>
/// <para>
/// <see cref="SearchVector"/> is a <c>GENERATED ALWAYS AS ... STORED</c> column, so PostgreSQL
/// maintains it in the same transaction as the write. There is no interceptor, no outbox and
/// no reindex job, and the index cannot drift from the row it describes.
/// </para>
/// </summary>
public class SearchDocument
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    /// <summary>Provider/entity key: "vendor", "purchase_order", …</summary>
    public string EntityType { get; set; } = "";

    /// <summary>Human label, indexed rather than derived client-side so a new type needs no
    /// front-end change to be labelled correctly.</summary>
    public string Label { get; set; } = "";

    public long EntityId { get; set; }
    public int? DocNum { get; set; }

    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? Keywords { get; set; }

    /// <summary>Branch the source row belongs to, so results can be scope-filtered.</summary>
    public int BranchId { get; set; }
    public DateOnly? DocDate { get; set; }

    /// <summary>Maintained by PostgreSQL. Never assigned in code.</summary>
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}
