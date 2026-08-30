using Alkanzi.Erp.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alkanzi.Erp.DataAccess.Configurations;

/*
   Security-module tables carry the SM_ prefix, matching the existing ERP.

   The names are written in lower case here — sm_companies, not SM_COMPANIES — because of how
   PostgreSQL folds identifiers. An unquoted identifier folds to LOWER case, the opposite of
   Oracle, so a table actually named SM_COMPANIES exists only as the quoted "SM_COMPANIES" and
   every reference to it needs quotes forever: SELECT * FROM "SM_COMPANIES" works,
   SELECT * FROM SM_COMPANIES does not.

   Creating them lower case gets both: the table is sm_companies, and SELECT * FROM
   SM_COMPANIES still resolves to it because the unquoted name folds down. Uppercase SQL
   written out of Oracle habit keeps working.
*/

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> e)
    {
        e.ToTable("sm_organizations");
        e.Property(x => x.Code).HasMaxLength(20).IsRequired();
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();

        e.HasIndex(x => x.Code).IsUnique().HasFilter("is_deleted IS NOT TRUE");
    }
}

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> e)
    {
        e.ToTable("sm_companies");
        e.Property(x => x.Code).HasMaxLength(20).IsRequired();
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.Property(x => x.Currency).HasMaxLength(3).IsRequired();

        e.HasOne(x => x.Organization).WithMany(o => o.Companies)
         .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);

        // Scoped to the organization rather than global: two organizations may each run a
        // company coded "ALK", and a soft-deleted one must not reserve the code forever.
        e.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique().HasFilter("is_deleted IS NOT TRUE");
    }
}

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> e)
    {
        e.ToTable("sm_branches");
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
