using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// The typed <c>AddErpDbContext&lt;TContext&gt;</c> overload: the engine resolves
/// <see cref="ErpDbContext"/> and gets the consumer's subclass, which can map its
/// own approvable tables on top of the package's. Offline — model building needs
/// no live connection.
/// </summary>
public class ErpDbContextRegistrationTests
{
    // A consumer subclass that adds one approvable table to the model.
    private sealed class TestErpDbContext : ErpDbContext
    {
        public TestErpDbContext(DbContextOptions<TestErpDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);   // keep FM_TRANSACTION_MENU, log/workflow tables
            builder.Entity<ExtraApprovable>(e =>
            {
                e.HasKey(x => x.ID);
                e.ToTable("EXTRA_APPROVABLE");
                e.Property(x => x.ID).ValueGeneratedNever();
                e.HasQueryFilter(x => x.IS_DELETED != true);
            });
        }
    }

    private sealed class ExtraApprovable : IErpApprovable, IErpAuditable, IErpTenantScoped
    {
        public int ID { get; set; }
        public int? WORKFLOW_ID { get; set; }
        public int APPROVE_STATUS { get; set; }
        public int APPROVE_LEVEL { get; set; }
        public string? DIGIT_SIGNATURE { get; set; }
        public DateTime DOC_DATE { get; set; }
        public string? REMARKS { get; set; }
        public string? DOC_TYPE { get; set; }

        public int ORG_ID { get; set; }
        public int COMP_ID { get; set; }
        public int BRANCH_ID { get; set; }

        public bool? IS_UPDATED { get; set; }
        public bool? IS_DELETED { get; set; }
        public int CREATED_BY { get; set; }
        public int? UPDATED_BY { get; set; }
        public int? DELETED_BY { get; set; }
        public DateTime CREATED_AT { get; set; }
        public DateTime? UPDATED_AT { get; set; }
        public DateTime? DELETED_AT { get; set; }
    }

    [Fact]
    public void Generic_overload_registers_subclass_and_keeps_both_models()
    {
        var services = new ServiceCollection();
        services.AddErpDbContext<TestErpDbContext>("Data Source=unused;User Id=x;Password=y;");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

        // The engine depends on ErpDbContext and now gets the subclass instance.
        Assert.IsType<TestErpDbContext>(context);

        // Both the package's tables and the consumer's extra one are in the model,
        // so the engine can resolve the extra table by name.
        Assert.NotNull(context.Model.FindEntityType(typeof(FM_TRANSACTION_MENU)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ExtraApprovable)));
        Assert.Equal("EXTRA_APPROVABLE", context.Model.FindEntityType(typeof(ExtraApprovable))!.GetTableName());
    }
}
