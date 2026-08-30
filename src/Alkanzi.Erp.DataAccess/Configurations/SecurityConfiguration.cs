using Alkanzi.Erp.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alkanzi.Erp.DataAccess.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> e)
    {
        e.ToTable("companies");
        e.Property(x => x.Code).HasMaxLength(20).IsRequired();
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.Property(x => x.Currency).HasMaxLength(3).IsRequired();

        // Unique among live rows only, so a deleted company does not reserve its code forever.
        e.HasIndex(x => x.Code).IsUnique().HasFilter("is_deleted IS NOT TRUE");
    }
}

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> e)
    {
        e.ToTable("branches");
        e.Property(x => x.Code).HasMaxLength(20).IsRequired();
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();

        e.HasOne(x => x.Company).WithMany(c => c.Branches)
         .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

        // Codes are unique per company, not globally: two companies may both have a "HO".
        e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasFilter("is_deleted IS NOT TRUE");
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> e)
    {
        e.Property(x => x.FullName).HasMaxLength(200).IsRequired();

        e.HasOne(x => x.Company).WithMany()
         .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Branch).WithMany()
         .HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);

        // Company is soft-deleted and filtered, and a user's company is required. Without a
        // matching filter here, deleting a company would leave its users visible but with a
        // company that no query can load. Filtering them too makes the intent explicit:
        // retiring a company retires its accounts.
        //
        // The consequence is worth stating plainly — soft-deleting the last company hides
        // every user, administrators included, and the way back is to undelete the company,
        // not to create another account.
        e.HasQueryFilter(u => u.Company!.IS_DELETED != true);
    }
}
