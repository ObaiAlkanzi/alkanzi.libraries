using Alkanzi.Auditable.EntityFrameworkCore;
using Alkanzi.Erp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.Data;

public class ErpDbContext : DbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options) { }

    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<SearchDocument> SearchDocuments => Set<SearchDocument>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Vendor>(e =>
        {
            e.ToTable("vendors");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContactPerson).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Trn).HasMaxLength(50);

            // Partial unique index: a name is unique among LIVE rows only, so a soft-deleted
            // vendor does not permanently reserve its name. This is the kind of thing that
            // needs a filtered index — a plain unique constraint would block reuse forever.
            e.HasIndex(x => x.Name)
             .IsUnique()
             .HasFilter("is_deleted IS NOT TRUE");
        });

        b.Entity<PurchaseOrder>(e =>
        {
            e.ToTable("purchase_orders");
            e.HasKey(x => x.Id);

            // numeric(18,2), not double: money must not round in binary floating point.
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Remarks).HasMaxLength(2000);

            // Stored as its integer value so it matches the ERP's APPROVE_STATUS numbering
            // (0 draft / 3 rejected / 4 approved) rather than a string that would have to be
            // translated at the boundary.
            e.Property(x => x.Status).HasConversion<int>();

            e.HasOne(x => x.Vendor)
             .WithMany(v => v.PurchaseOrders)
             .HasForeignKey(x => x.VendorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.DocNum).IsUnique().HasFilter("is_deleted IS NOT TRUE");
            e.HasIndex(x => x.DocDate);
            e.HasIndex(x => new { x.Status, x.BranchId });
        });

        b.Entity<SearchDocument>(e =>
        {
            e.ToTable("search_documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Label).HasMaxLength(100).IsRequired();
            e.Property(x => x.Title).HasMaxLength(400).IsRequired();
            e.Property(x => x.Subtitle).HasMaxLength(400);

            e.HasIndex(x => new { x.EntityType, x.EntityId }).IsUnique();

            // The whole reason this is on PostgreSQL: the database derives the search vector
            // from the text columns, in the same transaction as the write. It cannot go stale.
            //
            // Written as raw SQL rather than HasGeneratedTsVectorColumn because that helper
            // concatenates the columns into one to_tsvector call, which makes every match
            // equally relevant. Weighting each source separately is what lets ts_rank put a
            // title hit above an incidental keyword hit: A = title, B = subtitle, C = the
            // keyword blob.
            //
            // 'simple' rather than 'english': ERP content is largely proper nouns, codes and
            // document numbers, where English stemming does more harm than good. Switch to a
            // language configuration only if the content is genuinely prose.
            e.Property(x => x.SearchVector)
             .HasColumnType("tsvector")
             .HasComputedColumnSql(
                 "setweight(to_tsvector('simple', coalesce(title, '')), 'A') || " +
                 "setweight(to_tsvector('simple', coalesce(subtitle, '')), 'B') || " +
                 "setweight(to_tsvector('simple', coalesce(keywords, '')), 'C')",
                 stored: true);

            e.HasIndex(x => x.SearchVector).HasMethod("GIN");
        });

        // Hides soft-deleted rows from every query. Called last, after the configuration
        // above, because it skips entity types that already declare a filter.
        b.ApplyAuditableQueryFilters();
    }
}
