namespace Alkanzi.DataAccess;

/// <summary>
/// One row of the unified search index (table SEARCH_INDEX). Every searchable record from any
/// source table is projected into this uniform shape, so a single query can search across all
/// entity types at once, ranked and permission-filtered. In the ERP this is kept in sync by the
/// SaveChanges interceptor; in the demo it is rebuilt from the source tables.
/// </summary>
public class SearchDocument
{
    public int Id { get; set; }

    /// <summary>Provider/entity key: "inventory", "call", "vendor", "customer", …</summary>
    public string EntityType { get; set; } = "";

    /// <summary>The source record's id.</summary>
    public long EntityId { get; set; }

    /// <summary>Document number where applicable (LPO/call), for id-style lookups.</summary>
    public int? DocNum { get; set; }

    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }

    /// <summary>All searchable text, flattened and upper-cased for case-insensitive matching.</summary>
    public string Keywords { get; set; } = "";

    /// <summary>Branch for row-level permission filtering (0 = global/always visible).</summary>
    public int BranchId { get; set; }

    /// <summary>Department for role-based scoping (0 = none).</summary>
    public int DepartmentId { get; set; }

    public DateTime? DocDate { get; set; }
}
