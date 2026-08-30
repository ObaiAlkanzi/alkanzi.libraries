using Alkanzi.Erp.DataAccess.Search;
using Alkanzi.Erp.Domain.Procurement;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.DataAccess;

/// <summary>
/// Development sample data for the procurement module, so a fresh database is immediately
/// usable. Idempotent — it does nothing once vendors exist.
/// </summary>
public static class DevSeed
{
    public static async Task SeedAsync(ErpDbContext db, CancellationToken ct = default)
    {
        if (await db.Vendors.AnyAsync(ct).ConfigureAwait(false)) return;

        var company = await db.Companies.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (company is null) return;   // security seed has not run; nothing to attach data to

        var branches = await db.Branches.Where(b => b.CompanyId == company.Id).OrderBy(b => b.Id).ToListAsync(ct).ConfigureAwait(false);
        if (branches.Count == 0) return;

        var vendors = new[]
        {
            new Vendor { CompanyId = company.Id, Name = "Crystal Trading FZE",          ContactPerson = "Rami Haddad",   Email = "rami@crystal.ae",   Phone = "+971 4 555 0101", Trn = "100234567800003", BranchId = branches[0].Id },
            new Vendor { CompanyId = company.Id, Name = "Golden Aluminium Est.",        ContactPerson = "Sara Nasser",   Email = "sara@golden.ae",    Phone = "+971 4 555 0102", Trn = "100234567800004", BranchId = branches[0].Id },
            new Vendor { CompanyId = company.Id, Name = "Star Hardware Trading Co.",    ContactPerson = "Imran Qureshi", Email = "imran@starhw.ae",   Phone = "+971 4 555 0103", Trn = "100234567800005", BranchId = branches[1 % branches.Count].Id },
            new Vendor { CompanyId = company.Id, Name = "Falcon Sanitary Ware Trading", ContactPerson = "Dana Aziz",     Email = "dana@falcon.ae",    Phone = "+971 4 555 0104", Trn = "100234567800006", BranchId = branches[1 % branches.Count].Id },
            new Vendor { CompanyId = company.Id, Name = "Emirates Electricals Trading", ContactPerson = "Yousef Karim",  Email = "yousef@emel.ae",    Phone = "+971 4 555 0105", Trn = "100234567800007", BranchId = branches[2 % branches.Count].Id },
            new Vendor { CompanyId = company.Id, Name = "National Trading Co.",         ContactPerson = "Laila Mansour", Email = "laila@national.ae", Phone = "+971 4 555 0106", Trn = "100234567800008", BranchId = branches[2 % branches.Count].Id },
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
                CompanyId = company.Id,
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

        // No explicit rebuild here any more: SearchIndexInterceptor indexes these rows as
        // they are saved. RebuildAsync remains for the case it is actually for — repairing
        // the index after a bulk import that bypassed EF.
    }
}
