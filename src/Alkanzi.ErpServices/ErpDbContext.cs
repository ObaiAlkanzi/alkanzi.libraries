using Microsoft.EntityFrameworkCore;

namespace Alkanzi.ErpServices;

/// <summary>
/// A context over the real ERP tables the services touch.
/// </summary>
/// <remarks>
/// Maps the document-type registry and the transaction headers dispatched to,
/// hides soft-deleted rows with its own query filters, and attaches the
/// project's audit interceptor when one is supplied. Self-contained: nothing
/// here comes from an external auditing library.
/// </remarks>
public class ErpDbContext : DbContext
{
    private readonly ErpAuditSaveChangesInterceptor? _auditInterceptor;

    /// <summary>Creates the context, optionally attaching the audit interceptor.</summary>
    public ErpDbContext(
        DbContextOptions<ErpDbContext> options,
        ErpAuditSaveChangesInterceptor? auditInterceptor = null)
        : base(options)
        => _auditInterceptor = auditInterceptor;

    /// <summary>The document-type registry.</summary>
    public DbSet<FM_TRANSACTION_MENU> TransactionMenus => Set<FM_TRANSACTION_MENU>();

    /// <summary>Journal voucher headers.</summary>
    public DbSet<FM_JOURNAL_HDR> Journals => Set<FM_JOURNAL_HDR>();

    /// <summary>Call registration headers.</summary>
    public DbSet<CALL_REGISTERATION> CallRegistrations => Set<CALL_REGISTERATION>();

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Guarded: when the context is configured through AddDbContext the
        // interceptor is usually added there instead, and adding it twice would
        // stamp every row twice.
        if (_auditInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(_auditInterceptor);
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FM_TRANSACTION_MENU>(entity =>
        {
            entity.HasKey(e => e.ID);
            entity.ToTable("FM_TRANSACTION_MENU");

            entity.Property(e => e.ID).HasColumnName("ID").ValueGeneratedNever();
            entity.Property(e => e.DOC_TYPE).HasColumnName("DOC_TYPE");
            entity.Property(e => e.ORG_ID).HasColumnName("ORG_ID");
            entity.Property(e => e.COMP_ID).HasColumnName("COMP_ID");
            entity.Property(e => e.BRANCH_ID).HasColumnName("BRANCH_ID");
            entity.Property(e => e.TABLE_NAME).HasColumnName("TABLE_NAME");
        });

        modelBuilder.Entity<FM_JOURNAL_HDR>(entity =>
        {
            entity.HasKey(e => e.ID);
            entity.ToTable("FM_JOURNAL_HDR");
            entity.Property(e => e.ID).HasColumnName("ID").ValueGeneratedNever();

            // Not equality with false: a null IS_DELETED (rows predating the
            // audit columns) must stay visible, and only an explicit true hides.
            entity.HasQueryFilter(e => e.IS_DELETED != true);
        });

        modelBuilder.Entity<CALL_REGISTERATION>(entity =>
        {
            entity.HasKey(e => e.ID);
            entity.ToTable("CALL_REGISTERATION");
            entity.Property(e => e.ID).HasColumnName("ID").ValueGeneratedNever();

            entity.HasQueryFilter(e => e.IS_DELETED != true);
        });
    }
}
