using Alkanzi.Erp.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alkanzi.Erp.DataAccess.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> e)
    {
        e.ToTable("vendors");
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.Property(x => x.ContactPerson).HasMaxLength(150);
        e.Property(x => x.Email).HasMaxLength(200);
        e.Property(x => x.Phone).HasMaxLength(50);
        e.Property(x => x.Trn).HasMaxLength(50);

        // Scoped to the company: two companies may legitimately deal with the same supplier
        // name, and a soft-deleted vendor must not reserve it.
        e.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique().HasFilter("is_deleted IS NOT TRUE");
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> e)
    {
        e.ToTable("purchase_orders");

        // numeric(18,2), not double: money must not round in binary floating point.
        e.Property(x => x.Amount).HasPrecision(18, 2);
        e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        e.Property(x => x.Remarks).HasMaxLength(2000);

        // Persisted as its integer so it matches the ERP's APPROVE_STATUS numbering rather
        // than a string needing translation at the boundary.
        e.Property(x => x.Status).HasConversion<int>();

        e.HasOne(x => x.Vendor).WithMany(v => v.PurchaseOrders)
         .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Branch).WithMany()
         .HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => new { x.CompanyId, x.DocNum }).IsUnique().HasFilter("is_deleted IS NOT TRUE");
        e.HasIndex(x => x.DocDate);

        // Covers the dashboard's "pending in my branches" filter.
        e.HasIndex(x => new { x.Status, x.BranchId });
    }
}
