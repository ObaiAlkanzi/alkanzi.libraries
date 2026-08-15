using Microsoft.EntityFrameworkCore;
using Modules_DataTables.CALL_MODULES;
using Modules_DataTables.IM_MODULES;

namespace Alkanzi.SearchEngine.Erp;

/// <summary>
/// A minimal, read-only DbContext for search. It maps only the entities the search
/// providers query (both pure-POCO transaction entities), so it stays independent of
/// the full ERP DbContext. Configure it against Oracle via <c>AddErpSearch</c>.
/// </summary>
public class ErpSearchDbContext : DbContext
{
    public ErpSearchDbContext(DbContextOptions<ErpSearchDbContext> options) : base(options) { }

    public DbSet<IM_PURCHASE_ORDERS> IM_PURCHASE_ORDERS => Set<IM_PURCHASE_ORDERS>();
    public DbSet<CALL_REGISTERATION> CALL_REGISTERATION => Set<CALL_REGISTERATION>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IM_PURCHASE_ORDERS>(e =>
        {
            e.ToTable("IM_PURCHASE_ORDERS");
            e.HasKey(x => x.ID);
        });

        modelBuilder.Entity<CALL_REGISTERATION>(e =>
        {
            e.ToTable("CALL_REGISTERATION");
            e.HasKey(x => x.ID);
        });

        base.OnModelCreating(modelBuilder);
    }
}
