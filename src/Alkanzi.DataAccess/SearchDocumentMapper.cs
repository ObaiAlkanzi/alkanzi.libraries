using Modules_DataTables.CALL_MODULES;
using Modules_DataTables.IM_MODULES;
using Modules_DataTables.PM_MODULES;

namespace Alkanzi.DataAccess;

/// <summary>
/// The single source of truth for turning a source entity into a <see cref="SearchDocument"/>.
/// Used by both the bulk rebuild (<see cref="SearchIndexBuilder"/>) and the live
/// <see cref="SearchIndexInterceptor"/>, so the index shape never diverges.
/// </summary>
public static class SearchDocumentMapper
{
    /// <summary>The index key (entity type + id) for any searchable entity, or null if not searchable.</summary>
    public static (string Type, long Id)? Key(object entity) => entity switch
    {
        IM_PURCHASE_ORDERS x => ("inventory", (long)x.ID),
        CALL_REGISTERATION x => ("call", (long)x.ID),
        FM_SUPPLIER_MASTER x => ("vendor", (long)x.ID),
        FM_CUSTOMER_MASTER x => ("customer", (long)x.ID),
        _ => (( string, long )?)null,
    };

    public static bool IsSearchable(object entity) => Key(entity) is not null;

    /// <summary>
    /// Projects an entity to a search document, or returns null when it must NOT be indexed
    /// (not a searchable type, soft-deleted, or a non-LPO purchase-order row).
    /// </summary>
    public static SearchDocument? Map(object entity) => entity switch
    {
        IM_PURCHASE_ORDERS x when !x.IS_DELETED && x.DOC_TYPE == "imPurchaseOrder" => new SearchDocument
        {
            EntityType = "inventory",
            EntityId = x.ID,
            DocNum = x.DOC_NUM,
            Title = $"LPO-{x.ID}",
            Subtitle = x.ACCOUNT_NAME,
            Keywords = Kw(x.ACCOUNT_NAME, x.ACCOUNT_REMARKS, x.CUST_REMARKS, x.REMARKS, x.SYS_REMARKS, x.DOC_NUM.ToString()),
            BranchId = x.BRANCH_ID,
            DocDate = x.DOC_DATE,
        },
        CALL_REGISTERATION x when !x.IS_DELETED => new SearchDocument
        {
            EntityType = "call",
            EntityId = x.ID,
            DocNum = x.DOC_NUM,
            Title = $"Call {x.ID}",
            Subtitle = x.NAME,
            Keywords = Kw(x.NAME, x.CALL_NO, x.MOBILE, x.SUPERVISOR_NOTE, x.EXTER_BUILDING, x.EXTER_LOCATION, x.EMAIL, x.DOC_NUM.ToString()),
            BranchId = x.BRANCH_ID,
            DocDate = x.DOC_DATE,
        },
        FM_SUPPLIER_MASTER x when !x.IS_DELETED => new SearchDocument
        {
            EntityType = "vendor",
            EntityId = x.ID,
            Title = string.IsNullOrWhiteSpace(x.NAME) ? $"Vendor {x.ID}" : x.NAME,
            Subtitle = "Vendor",
            Keywords = Kw(x.NAME, x.DISPLAY_NAME, x.EMAIL, x.ADDRESS, x.EMIRATES_ID, x.CONTACT_PERSON, x.PASSPORT_NO),
            BranchId = 0,
        },
        FM_CUSTOMER_MASTER x when !x.IS_DELETED => new SearchDocument
        {
            EntityType = "customer",
            EntityId = x.ID,
            Title = string.IsNullOrWhiteSpace(x.NAME) ? $"Customer {x.ID}" : x.NAME,
            Subtitle = "Customer",
            Keywords = Kw(x.NAME, x.DISPLAY_NAME, x.EMAIL, x.ADDRESS, x.EMIRATES_ID, x.CONTACT_PERSON, x.PASSPORT_NO, x.TRN),
            BranchId = 0,
        },
        _ => null,
    };

    private static string Kw(params string?[] parts) =>
        string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))).ToUpperInvariant();
}
