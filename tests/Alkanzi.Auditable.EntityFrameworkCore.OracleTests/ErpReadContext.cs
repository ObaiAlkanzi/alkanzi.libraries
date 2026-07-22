using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Auditable.EntityFrameworkCore.OracleTests;

/// <summary>
/// Read-only view of the real <c>FM_TRANSACTION_MENU</c>.
/// </summary>
/// <remarks>
/// Only the columns dispatch needs are mapped. Two reasons: EF then selects
/// just those, so the rest of the table's shape cannot break the query; and
/// nothing here can accidentally write a column it does not know about.
/// </remarks>
public class FM_TRANSACTION_MENU : ITransactionMenu
{
    public int ID { get; set; }
    public string DOC_TYPE { get; set; } = string.Empty;
    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int? BRANCH_ID { get; set; }
    public string? TABLE_NAME { get; set; }
}

/// <summary>
/// Read-only view of the real <c>FM_JOURNAL_HDR</c> — the table
/// <c>FM_TRANSACTION_MENU</c> names for document type <c>JournalVoucher</c>.
/// </summary>
/// <remarks>
/// Present so dispatch has somewhere to land: the engine resolves TABLE_NAME
/// through EF's model, so a target table absent from the context fails with
/// "No entity type is mapped to table 'FM_JOURNAL_HDR'" however much data the
/// schema holds. Implements <see cref="IAuditable"/> because the table carries
/// the full audit column set, which is what keeps soft-deleted journals from
/// being dispatched.
/// </remarks>
public class FM_JOURNAL_HDR : IApprovable, IAuditable
{
    public int ID { get; set; }
    public int JV_NO { get; set; }
    public string? DOC_TYPE { get; set; }
    public int DOC_NUM { get; set; }
    public string? NARRATION { get; set; }

    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int BRANCH_ID { get; set; }

    // NUMBER(1) NOT NULL in Oracle; nullable here because IAuditable declares it
    // so, and a non-null column reads into a nullable property without trouble.
    public bool? IS_UPDATED { get; set; }
    public bool? IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
    public int APPROVE_STATUS { get; set; }
    public int APPROVE_LEVEL { get; set; }
    public string? DIGIT_SIGNATURE { get; set; }
}

public class CALL_REGISTERATION:IApprovable, IAuditable, IWorkflowBound
{
    public int ID { get; set; }
    public bool? IS_UPDATED { get ; set; }
    public bool? IS_DELETED { get;set; }
    public int CREATED_BY { get;set; }
    public int? UPDATED_BY { get;set; }
    public int? DELETED_BY { get;set; }
    public DateTime CREATED_AT { get;set; }
    public DateTime? UPDATED_AT { get;set; }
    public DateTime? DELETED_AT { get;set; }
    public int APPROVE_STATUS { get;set; }
    public int APPROVE_LEVEL { get;set; }
    public string? DIGIT_SIGNATURE { get;set; }
    public int? WORKFLOW_ID { get;set; }
    // ...only the columns you actually want to assert on
}

/// <summary>Fixed tenant for a test.</summary>
public sealed class FixedCompany(int org, int comp, int? branch) : ICompanyContext
{
    public int ORG_ID { get; } = org;
    public int COMP_ID { get; } = comp;
    public int? BRANCH_ID { get; } = branch;
}

/// <summary>
/// A context over the ERP's own tables, kept separate from
/// <see cref="OracleDbContext"/> on purpose.
/// </summary>
/// <remarks>
/// <see cref="OracleFixture"/> drops and recreates the tables in its model.
/// Keeping the real ERP tables in a different context guarantees they can never
/// be caught by that: this one is only ever queried.
/// </remarks>
public sealed class ErpReadContext(DbContextOptions<ErpReadContext> options) : DbContext(options)
{
    public DbSet<FM_TRANSACTION_MENU> TransactionMenus => Set<FM_TRANSACTION_MENU>();
    public DbSet<FM_JOURNAL_HDR> Journals => Set<FM_JOURNAL_HDR>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FM_TRANSACTION_MENU>(entity =>
        {
            // Keyless: this is a read-only projection, and declaring no key
            // keeps EF from doing identity resolution over rows it will never
            // track or save.
            entity.HasNoKey();
            entity.ToTable("FM_TRANSACTION_MENU");

            entity.Property(e => e.ID).HasColumnName("ID");
            entity.Property(e => e.DOC_TYPE).HasColumnName("DOC_TYPE");
            entity.Property(e => e.ORG_ID).HasColumnName("ORG_ID");
            entity.Property(e => e.COMP_ID).HasColumnName("COMP_ID");
            entity.Property(e => e.BRANCH_ID).HasColumnName("BRANCH_ID");
            entity.Property(e => e.TABLE_NAME).HasColumnName("TABLE_NAME");
        });

        modelBuilder.Entity<FM_JOURNAL_HDR>(entity =>
        {
            // Keyed, unlike the menu above: dispatch looks a row up by primary
            // key, and EntityResolver throws outright on a keyless target.
            entity.HasKey(e => e.ID);
            entity.ToTable("FM_JOURNAL_HDR");

            entity.Property(e => e.ID).HasColumnName("ID").ValueGeneratedNever();
            entity.Property(e => e.JV_NO).HasColumnName("JV_NO");
            entity.Property(e => e.DOC_TYPE).HasColumnName("DOC_TYPE");
            entity.Property(e => e.DOC_NUM).HasColumnName("DOC_NUM");
            entity.Property(e => e.NARRATION).HasColumnName("NARRATION");
            entity.Property(e => e.ORG_ID).HasColumnName("ORG_ID");
            entity.Property(e => e.COMP_ID).HasColumnName("COMP_ID");
            entity.Property(e => e.BRANCH_ID).HasColumnName("BRANCH_ID");
        });

        // in OnModelCreating
        modelBuilder.Entity<CALL_REGISTERATION>(entity =>
        {
            entity.HasKey(e => e.ID);          // required — resolver throws without a PK
            entity.ToTable("CALL_REGISTERATION");
            entity.Property(e => e.ID).HasColumnName("ID").ValueGeneratedNever();
        });
        // Same soft-delete semantics the library gives consumers, so a deleted
        // journal is invisible here too rather than only to the resolver.
        modelBuilder.ApplyAuditableQueryFilters();
    }
}
