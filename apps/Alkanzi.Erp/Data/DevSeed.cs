using Alkanzi.Erp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.Data;

/// <summary>
/// Development seed data, so a fresh database is immediately usable. Idempotent: it does
/// nothing when vendors already exist, so restarting the app never duplicates rows.
/// </summary>
public static class DevSeed
{
    public static async Task SeedAsync(ErpDbContext db, CancellationToken ct = default)
    {
        if (await db.Vendors.AnyAsync(ct).ConfigureAwait(false)) return;

        var vendors = new[]
        {
            new Vendor { Name = "Crystal Trading FZE",          ContactPerson = "Rami Haddad",   Email = "rami@crystal.ae",   Phone = "+971 4 555 0101", Trn = "100234567800003", BranchId = 1 },
            new Vendor { Name = "Golden Aluminium Est.",        ContactPerson = "Sara Nasser",   Email = "sara@golden.ae",    Phone = "+971 4 555 0102", Trn = "100234567800004", BranchId = 1 },
            new Vendor { Name = "Star Hardware Trading Co.",    ContactPerson = "Imran Qureshi", Email = "imran@starhw.ae",   Phone = "+971 4 555 0103", Trn = "100234567800005", BranchId = 2 },
            new Vendor { Name = "Falcon Sanitary Ware Trading", ContactPerson = "Dana Aziz",     Email = "dana@falcon.ae",    Phone = "+971 4 555 0104", Trn = "100234567800006", BranchId = 2 },
            new Vendor { Name = "Emirates Electricals Trading", ContactPerson = "Yousef Karim",  Email = "yousef@emel.ae",    Phone = "+971 4 555 0105", Trn = "100234567800007", BranchId = 3 },
            new Vendor { Name = "National Trading Co.",         ContactPerson = "Laila Mansour", Email = "laila@national.ae", Phone = "+971 4 555 0106", Trn = "100234567800008", BranchId = 3 },
        };
        db.Vendors.AddRange(vendors);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var statuses = new[] { ApprovalStatus.Approved, ApprovalStatus.Pending, ApprovalStatus.Draft, ApprovalStatus.Rejected };
        var orders = new List<PurchaseOrder>();
        var docNum = 5000;

        for (var i = 0; i < 48; i++)
        {
            var vendor = vendors[i % vendors.Length];
            orders.Add(new PurchaseOrder
            {
                DocNum = ++docNum,
                DocDate = today.AddDays(-i * 2),
                VendorId = vendor.Id,
                Amount = 4_500m + (i * 1_375m),
                Currency = "AED",
                Status = statuses[i % statuses.Length],
                BranchId = vendor.BranchId,
                Remarks = i % 3 == 0 ? "Site delivery required" : null,
            });
        }
        db.PurchaseOrders.AddRange(orders);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await SearchIndexer.RebuildAsync(db, ct).ConfigureAwait(false);
    }
}
