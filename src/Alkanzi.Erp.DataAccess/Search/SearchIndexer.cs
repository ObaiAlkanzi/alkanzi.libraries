using Alkanzi.Erp.Domain.Procurement;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.DataAccess.Search;

/// <summary>
/// Projects source rows into <see cref="SearchDocument"/> rows.
/// <para>
/// One place decides what a document looks like in the index, so the bulk rebuild and any
/// incremental update cannot produce different shapes for the same record. Note what is
/// <i>not</i> here: nothing computes a search vector. PostgreSQL derives that from the text
/// columns as they are written.
/// </para>
/// </summary>
public static class SearchIndexer
{
    public static SearchDocument ToDocument(Vendor v) => new()
    {
        EntityType = "vendor",
        CompanyId = v.CompanyId,
        Label = "Vendor",
        EntityId = v.Id,
        Title = v.Name,
        Subtitle = v.ContactPerson,
        Keywords = Keywords(v.Name, v.ContactPerson, v.Email, v.Phone, v.Trn),
        BranchId = v.BranchId,
    };

    public static SearchDocument ToDocument(PurchaseOrder p, string? vendorName) => new()
    {
        EntityType = "purchase_order",
        CompanyId = p.CompanyId,
        Label = "Purchase Order",
        EntityId = p.Id,
        DocNum = p.DocNum,
        Title = $"LPO-{p.DocNum}",
        Subtitle = vendorName,
        Keywords = Keywords(vendorName, p.Remarks, p.DocNum.ToString(), p.Status.ToString(), p.Currency),
        BranchId = p.BranchId,
        DocDate = p.DocDate,
    };

    /// <summary>
    /// Clears and repopulates the whole index from the source tables. Suitable after a bulk
    /// import that bypassed the application; ordinary writes should update their own document.
    /// </summary>
    public static async Task<int> RebuildAsync(ErpDbContext db, CancellationToken ct = default)
    {
        await db.SearchDocuments.ExecuteDeleteAsync(ct).ConfigureAwait(false);

        var vendors = await db.Vendors.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        var orders = await db.PurchaseOrders.AsNoTracking()
            .Select(p => new { Order = p, VendorName = p.Vendor!.Name })
            .ToListAsync(ct).ConfigureAwait(false);

        var docs = new List<SearchDocument>(vendors.Count + orders.Count);
        docs.AddRange(vendors.Select(ToDocument));
        docs.AddRange(orders.Select(o => ToDocument(o.Order, o.VendorName)));

        db.SearchDocuments.AddRange(docs);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return docs.Count;
    }

    private static string Keywords(params string?[] parts) =>
        string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}
