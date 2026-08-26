using Microsoft.EntityFrameworkCore;

namespace Alkanzi.DataAccess;

/// <summary>
/// Builds/rebuilds the unified <c>SEARCH_INDEX</c> from the source tables. A full rebuild is used
/// after bulk imports (which bypass EF); ongoing changes are kept live by
/// <see cref="SearchIndexInterceptor"/>. Both use <see cref="SearchDocumentMapper"/> so the shape matches.
/// </summary>
public static class SearchIndexBuilder
{
    /// <summary>Creates the SEARCH_INDEX table if it doesn't exist yet (no drop of anything else).</summary>
    public static void EnsureTable(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('SEARCH_INDEX','U') IS NULL
BEGIN
    CREATE TABLE [SEARCH_INDEX] (
        [Id]           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EntityType]   NVARCHAR(40)  NOT NULL,
        [EntityId]     BIGINT        NOT NULL,
        [DocNum]       INT           NULL,
        [Title]        NVARCHAR(400) NOT NULL,
        [Subtitle]     NVARCHAR(400) NULL,
        [Keywords]     NVARCHAR(MAX) NOT NULL,
        [BranchId]     INT           NOT NULL,
        [DepartmentId] INT           NOT NULL,
        [DocDate]      DATETIME2     NULL
    );
    CREATE INDEX IX_SEARCH_INDEX_Type   ON [SEARCH_INDEX]([EntityType]);
    CREATE INDEX IX_SEARCH_INDEX_TypeId ON [SEARCH_INDEX]([EntityType],[EntityId]);
END");
    }

    public static int Count(AppDbContext db) { EnsureTable(db); return db.SearchIndex.Count(); }

    /// <summary>Clears and repopulates the whole index from the source tables.</summary>
    public static int Rebuild(AppDbContext db)
    {
        EnsureTable(db);
        db.Database.ExecuteSqlRaw("DELETE FROM [SEARCH_INDEX]");

        var docs = new List<SearchDocument>(20000);
        docs.AddRange(db.IM_PURCHASE_ORDERS.AsNoTracking().Where(x => !x.IS_DELETED && x.DOC_TYPE == "imPurchaseOrder").AsEnumerable().Select(SearchDocumentMapper.Map).OfType<SearchDocument>());
        docs.AddRange(db.CALL_REGISTERATION.AsNoTracking().Where(x => !x.IS_DELETED).AsEnumerable().Select(SearchDocumentMapper.Map).OfType<SearchDocument>());
        docs.AddRange(db.FM_SUPPLIER_MASTER.AsNoTracking().Where(x => !x.IS_DELETED).AsEnumerable().Select(SearchDocumentMapper.Map).OfType<SearchDocument>());
        docs.AddRange(db.FM_CUSTOMER_MASTER.AsNoTracking().Where(x => !x.IS_DELETED).AsEnumerable().Select(SearchDocumentMapper.Map).OfType<SearchDocument>());

        var previous = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            db.SearchIndex.AddRange(docs);
            db.SaveChanges();
        }
        finally { db.ChangeTracker.AutoDetectChangesEnabled = previous; }

        return docs.Count;
    }
}
