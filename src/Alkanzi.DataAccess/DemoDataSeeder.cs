using Modules_DataTables.CALL_MODULES;
using Modules_DataTables.IM_MODULES;
using Modules_DataTables.PM_MODULES;

namespace Alkanzi.DataAccess;

/// <summary>
/// Seeds a large, deterministic sample dataset so the workspace and the paged search screen
/// have realistic volume out of the box. A handful of recognizable "anchor" rows come first
/// (ACME, Gulf Steel…), followed by hundreds of generated rows. Replace with real data by
/// dropping the database and loading your own.
/// </summary>
public static class DemoDataSeeder
{
    // Tune these to grow/shrink the demo volume.
    private const int VendorCount = 300;
    private const int LpoCount = 700;
    private const int CallCount = 500;

    public static void Seed(AppDbContext db)
    {
        if (db.FM_SUPPLIER_MASTER.Any() || db.IM_PURCHASE_ORDERS.Any() || db.CALL_REGISTERATION.Any())
            return;

        var today = DateTime.Today;

        // ---------- Vendors ----------
        var vendorNames = new List<string>
        {
            "ACME Trading LLC", "Najmat Al Rolla Bldg. Materials", "Gulf Steel Supplies",
            "Emirates Hardware & Tools", "Al Futtaim Electricals", "Desert Rose Sanitary Ware",
            "Blue Nile Aluminium",
        };
        for (int i = vendorNames.Count; i < VendorCount; i++)
            vendorNames.Add($"{VPrefix[i % VPrefix.Length]} {VCore[(i / VPrefix.Length) % VCore.Length]} {VTail[i % VTail.Length]}");

        var vendors = new List<FM_SUPPLIER_MASTER>(vendorNames.Count);
        for (int i = 0; i < vendorNames.Count; i++)
            vendors.Add(new FM_SUPPLIER_MASTER { NAME = vendorNames[i], BRANCH_ID = (i % 3) + 1 });
        db.FM_SUPPLIER_MASTER.AddRange(vendors);

        // ---------- Purchase Orders (accounts drawn from the vendor pool) ----------
        var lpos = new List<IM_PURCHASE_ORDERS>(LpoCount);
        for (int i = 0; i < LpoCount; i++)
        {
            var account = vendorNames[i % vendorNames.Count];
            lpos.Add(new IM_PURCHASE_ORDERS
            {
                DOC_TYPE = "imPurchaseOrder",
                DOC_NUM = 5001 + i,
                ACCOUNT_NAME = account,
                BRANCH_ID = (i % 3) + 1,
                DOC_DATE = today.AddDays(-(i % 365)),
                ORDER_DATE = today.AddDays(-(i % 365)),
                DOC_STATUS = (i % 2 == 0) ? 2 : 1,
                APPROVE_STATUS = (i % 3 == 0) ? 0 : 1,
            });
        }
        db.IM_PURCHASE_ORDERS.AddRange(lpos);

        // ---------- Calls ----------
        var calls = new List<CALL_REGISTERATION>(CallCount);
        for (int i = 0; i < CallCount; i++)
        {
            var name = $"{CUnit[i % CUnit.Length]} {100 + (i % 900)} - {CArea[(i / CUnit.Length) % CArea.Length]}";
            calls.Add(new CALL_REGISTERATION
            {
                NAME = name,
                DOC_NUM = 9001 + i,
                BRANCH_ID = (i % 3) + 1,
                DOC_DATE = today.AddDays(-(i % 365)),
                FROM_DATE = today.AddDays(-(i % 365)),
            });
        }
        db.CALL_REGISTERATION.AddRange(calls);

        db.SaveChanges();
    }

    private static readonly string[] VPrefix =
        { "Al Futtaim", "Gulf", "Emirates", "Desert", "Blue Nile", "Oasis", "Falcon", "Pearl",
          "Rainbow", "Union", "National", "Prime", "Royal", "Metro", "Star", "Delta", "Orient",
          "Golden", "Silver", "Crystal" };

    private static readonly string[] VCore =
        { "Trading", "Building Materials", "Electricals", "Sanitary Ware", "Aluminium", "Hardware",
          "Steel", "Contracting", "Supplies", "Interiors", "Enterprises", "Industries", "Logistics", "Group" };

    private static readonly string[] VTail =
        { "LLC", "Est.", "Trading Co.", "FZE", "Intl.", "& Sons", "Co.", "Group" };

    private static readonly string[] CUnit =
        { "Villa", "Tower", "Warehouse", "Office", "Shop", "Flat", "Building", "Apartment", "Showroom", "Unit" };

    private static readonly string[] CArea =
        { "Al Barsha", "Business Bay", "Al Quoz", "JLT", "Deira", "Marina", "Al Nahda",
          "Silicon Oasis", "Al Rigga", "Bur Dubai", "Jumeirah", "Al Karama" };
}
