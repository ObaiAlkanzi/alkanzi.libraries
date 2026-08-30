using Alkanzi.Auditable.EntityFrameworkCore;
using Alkanzi.Erp.Domain.Procurement;
using Alkanzi.Erp.Domain.Security;
using Alkanzi.Erp.DataAccess.Search;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.DataAccess;

/// <summary>
/// The application's single database context.
/// <para>
/// Derives from <see cref="IdentityDbContext{TUser,TRole,TKey}"/> so Identity's tables and the
/// ERP's live in one database and one transaction. A separate identity context is a common
/// early split and a lasting mistake: creating a user and assigning their security groups
/// would stop being atomic, and every join from a document to its creator would cross a
/// context boundary.
/// </para>
/// </summary>
public class ErpDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options) { }

    // ---- security ----
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();

    // ---- procurement ----
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    // ---- search ----
    public DbSet<SearchDocument> SearchDocuments => Set<SearchDocument>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Identity's own mapping first — it configures the AspNet* tables and their keys.
        base.OnModelCreating(b);

        // Every IEntityTypeConfiguration in this assembly, so adding a module means adding a
        // configuration file rather than editing this method.
        b.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);

        // Identity's default table names are AspNetUsers, AspNetRoles, … Renamed to the
        // snake_case the rest of the schema uses, so hand-written SQL does not have to switch
        // quoting conventions halfway through a join.
        b.Entity<ApplicationUser>().ToTable("users");
        b.Entity<ApplicationRole>().ToTable("roles");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<int>>().ToTable("user_roles");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<int>>().ToTable("user_claims");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<int>>().ToTable("user_logins");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<int>>().ToTable("user_tokens");
        b.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>>().ToTable("role_claims");

        // Hides soft-deleted rows from every query. Last, because it skips entity types that
        // already declare a filter of their own.
        b.ApplyAuditableQueryFilters();
    }
}
