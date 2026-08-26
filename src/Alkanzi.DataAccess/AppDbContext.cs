using Microsoft.EntityFrameworkCore;
using Modules_DataTables.CALL_MODULES;
using Modules_DataTables.IM_MODULES;
using Modules_DataTables.PM_MODULES;

namespace Alkanzi.DataAccess;

/// <summary>
/// The demo/data-access context. Maps the procurement-workspace entities used by the
/// API and search. SQL Server for local dev (configured by the host).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<IM_PURCHASE_ORDERS> IM_PURCHASE_ORDERS => Set<IM_PURCHASE_ORDERS>();
    public DbSet<CALL_REGISTERATION> CALL_REGISTERATION => Set<CALL_REGISTERATION>();
    public DbSet<FM_SUPPLIER_MASTER> FM_SUPPLIER_MASTER => Set<FM_SUPPLIER_MASTER>();
    public DbSet<FM_CUSTOMER_MASTER> FM_CUSTOMER_MASTER => Set<FM_CUSTOMER_MASTER>();

    /// <summary>The unified search index (table SEARCH_INDEX).</summary>
    public DbSet<SearchDocument> SearchIndex => Set<SearchDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IM_PURCHASE_ORDERS>(e => { e.ToTable("IM_PURCHASE_ORDERS"); e.HasKey(x => x.ID); });
        modelBuilder.Entity<CALL_REGISTERATION>(e => { e.ToTable("CALL_REGISTERATION"); e.HasKey(x => x.ID); });
        modelBuilder.Entity<FM_SUPPLIER_MASTER>(e => { e.ToTable("FM_SUPPLIER_MASTER"); e.HasKey(x => x.ID); });
        modelBuilder.Entity<FM_CUSTOMER_MASTER>(e => { e.ToTable("FM_CUSTOMER_MASTER"); e.HasKey(x => x.ID); });
        modelBuilder.Entity<SearchDocument>(e =>
        {
            e.ToTable("SEARCH_INDEX");
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityType).HasMaxLength(40);
            e.Property(x => x.Title).HasMaxLength(400);
            e.Property(x => x.Subtitle).HasMaxLength(400);
            e.HasIndex(x => x.EntityType);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });
        base.OnModelCreating(modelBuilder);
    }
}
