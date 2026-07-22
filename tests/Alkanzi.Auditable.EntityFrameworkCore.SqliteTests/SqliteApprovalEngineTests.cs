namespace Alkanzi.Auditable.EntityFrameworkCore.SqliteTests;

/// <summary>
/// The engine against a private in-memory SQLite database, seeded per test.
/// Its counterpart, <c>OracleApprovalEngineTests</c>, runs the same lookups
/// read-only against the real ERP; the cases here are the ones that need rows
/// the ERP has no reason to hold.
/// </summary>
public class SqliteApprovalEngineTests
{
    private static readonly StubCompanyContext Company = new();

    private static ApprovalEngine<FM_TRANSACTION_MENU> EngineFor(
        TestDbContext context, ICompanyContext? company = null)
        => new(context, new EntityResolver(context), company ?? Company);

    /// <summary>
    /// A registry row for the tenant the engine is asked about. Seeds written
    /// without the tenant columns land on org/company 0 and no lookup finds them.
    /// </summary>
    private static FM_TRANSACTION_MENU MenuFor(string docType, string? tableName)
        => new()
        {
            DOC_TYPE = docType,
            TABLE_NAME = tableName,
            ORG_ID = Company.ORG_ID,
            COMP_ID = Company.COMP_ID,
            BRANCH_ID = Company.BRANCH_ID,
        };

    [Fact]
    public async Task Finds_the_menu_row_for_a_document_type()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Menus.Add(MenuFor("JournalVoucher", "FM_JOURNAL_HDR"));
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            var menu = await EngineFor(context).GetMenuAsync("JournalVoucher");

            Assert.Equal("FM_JOURNAL_HDR", menu?.TABLE_NAME);
        }
    }

    [Fact]
    public async Task Unknown_document_type_yields_null_menu()
    {
        using var fixture = new SqliteFixture();
        using var context = fixture.CreateContext();

        Assert.Null(await EngineFor(context).GetMenuAsync("NOPE"));
    }

    [Fact]
    public async Task Another_tenants_configuration_is_not_returned()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            // Same document type, different company: the registry holds one of
            // these per tenant, which is what makes the lookup single-valued.
            var other = MenuFor("PO", "Currencies");
            other.COMP_ID = Company.COMP_ID + 1;

            context.Menus.Add(MenuFor("PO", "Budgets"));
            context.Menus.Add(other);
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            var menu = await EngineFor(context).GetMenuAsync("PO");

            Assert.Equal("Budgets", menu?.TABLE_NAME);
        }
    }

    [Fact]
    public async Task Duplicate_document_types_are_an_error_not_a_coin_flip()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Menus.Add(MenuFor("PO", "Budgets"));
            context.Menus.Add(MenuFor("PO", "Currencies"));
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            // Two configurations claiming one document type within a single
            // tenant must surface, not silently route approvals to whichever
            // row came back first.
            await Assert.ThrowsAnyAsync<InvalidOperationException>(
                () => EngineFor(context).GetMenuAsync("PO"));
        }
    }

    [Fact]
    public async Task Loads_the_transaction_a_document_type_points_at()
    {
        using var fixture = new SqliteFixture();
        int transId;

        // Seeded, not read from the ERP: this fixture is a private in-memory
        // SQLite database created empty for each test. Coverage against the real
        // FM_TRANSACTION_MENU lives in the OracleTests project.
        using (var context = fixture.CreateContext())
        {
            context.Menus.Add(MenuFor("PO", "Budgets"));

            var budget = new Budget { Name = "Marketing" };
            context.Budgets.Add(budget);
            context.SaveChanges();
            transId = budget.Id;
        }

        using (var context = fixture.CreateContext())
        {
            var row = await EngineFor(context).GetTransactionByDocTypeAsync("PO", transId);

            Assert.Equal("Marketing", Assert.IsType<Budget>(row).Name);
        }
    }

    [Fact]
    public async Task Soft_deleted_transactions_are_not_returned()
    {
        using var fixture = new SqliteFixture();
        int transId;

        using (var context = fixture.CreateContext())
        {
            context.Menus.Add(MenuFor("PO", "Budgets"));

            var budget = new Budget { Name = "Dropped" };
            context.Budgets.Add(budget);
            context.SaveChanges();
            transId = budget.Id;

            context.Budgets.Remove(budget);
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            Assert.Null(await EngineFor(context).GetTransactionByDocTypeAsync("PO", transId));
        }
    }

    [Fact]
    public async Task Approval_state_is_readable_without_naming_the_entity_type()
    {
        using var fixture = new SqliteFixture();
        int transId;

        using (var context = fixture.CreateContext())
        {
            context.Menus.Add(MenuFor("PO", "Budgets"));

            var budget = new Budget { Name = "Marketing", APPROVE_STATUS = 2, APPROVE_LEVEL = 3 };
            context.Budgets.Add(budget);
            context.SaveChanges();
            transId = budget.Id;
        }

        using (var context = fixture.CreateContext())
        {
            var row = await EngineFor(context).GetApprovableByDocTypeAsync("PO", transId);

            Assert.NotNull(row);
            Assert.Equal(2, row!.APPROVE_STATUS);
            Assert.Equal(3, row.APPROVE_LEVEL);
        }
    }

    [Fact]
    public async Task Workflow_bound_rows_expose_their_workflow_through_the_interface()
    {
        using var fixture = new SqliteFixture();
        int transId;

        using (var context = fixture.CreateContext())
        {
            context.Menus.Add(MenuFor("WO", "WorkOrders"));

            var order = new WorkOrder { Title = "Rewire", APPROVE_STATUS = 1, WORKFLOW_ID = 77 };
            context.WorkOrders.Add(order);
            context.SaveChanges();
            transId = order.Id;
        }

        using (var context = fixture.CreateContext())
        {
            var row = await EngineFor(context).GetApprovableByDocTypeAsync("WO", transId);

            Assert.Equal(1, row?.APPROVE_STATUS);
            Assert.Equal(77, Assert.IsAssignableFrom<IWorkflowBound>(row).WORKFLOW_ID);
        }
    }

    [Fact]
    public async Task A_table_with_no_approval_columns_is_reported_rather_than_cast()
    {
        using var fixture = new SqliteFixture();
        int transId;

        using (var context = fixture.CreateContext())
        {
            // Currencies is dispatchable but carries no approval columns — the
            // shape seven real registry tables have.
            context.Menus.Add(MenuFor("FX", "Currencies"));

            var currency = new Currency { Code = "AED" };
            context.Currencies.Add(currency);
            context.SaveChanges();
            transId = currency.Id;
        }

        using (var context = fixture.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => EngineFor(context).GetApprovableByDocTypeAsync("FX", transId));

            Assert.Contains(nameof(IApprovable), ex.Message);
            Assert.Contains(nameof(Currency), ex.Message);
        }
    }

    [Fact]
    public async Task A_missing_approvable_row_is_null_not_an_error()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Menus.Add(MenuFor("PO", "Budgets"));
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            // Absent row must not be mistaken for "not approvable": the cast
            // check only applies to a row that came back.
            Assert.Null(await EngineFor(context).GetApprovableByDocTypeAsync("PO", 9999));
        }
    }

    /// <summary>
    /// Seeds one approvable transaction at a known point in the chain and
    /// returns its key, so a transition test only has to say where it started.
    /// </summary>
    private static int SeedBudget(SqliteFixture fixture, int status, int level)
    {
        using var context = fixture.CreateContext();

        context.Menus.Add(MenuFor("PO", "Budgets"));

        var budget = new Budget { Name = "Marketing", APPROVE_STATUS = status, APPROVE_LEVEL = level };
        context.Budgets.Add(budget);
        context.SaveChanges();

        return budget.Id;
    }

    [Theory]
    [InlineData(ApprovalAction.Submit, 1, 3)]   // climbs
    [InlineData(ApprovalAction.Approve, 4, 3)]  // climbs
    [InlineData(ApprovalAction.Reject, 3, 2)]   // frozen: records the level that refused
    public async Task Each_action_leaves_its_own_status_and_moves_the_level_its_own_way(
        ApprovalAction action, int expectedStatus, int expectedLevel)
    {
        using var fixture = new SqliteFixture();
        var transId = SeedBudget(fixture, status: 1, level: 2);

        using (var context = fixture.CreateContext())
        {
            var row = await EngineFor(context).ApplyApprovalAsync("PO", transId, action);

            Assert.Equal(expectedStatus, row.APPROVE_STATUS);
            Assert.Equal(expectedLevel, row.APPROVE_LEVEL);
        }

        // Read back through a fresh context: the point of the method is that it
        // persists, not merely that it mutated something tracked.
        using (var context = fixture.CreateContext())
        {
            var saved = Assert.IsType<Budget>(
                await EngineFor(context).GetTransactionByDocTypeAsync("PO", transId));

            Assert.Equal(expectedStatus, saved.APPROVE_STATUS);
            Assert.Equal(expectedLevel, saved.APPROVE_LEVEL);
        }
    }

    [Fact]
    public async Task Rework_drops_to_the_level_it_was_sent_back_to()
    {
        using var fixture = new SqliteFixture();
        var transId = SeedBudget(fixture, status: 1, level: 4);

        using var context = fixture.CreateContext();
        var row = await EngineFor(context).ApplyApprovalAsync("PO", transId, ApprovalAction.Rework, targetLevel: 1);

        Assert.Equal((int)ApprovalAction.Rework, row.APPROVE_STATUS);
        Assert.Equal(1, row.APPROVE_LEVEL);
    }

    [Fact]
    public async Task Rework_to_the_default_level_sends_it_back_to_the_start()
    {
        using var fixture = new SqliteFixture();
        var transId = SeedBudget(fixture, status: 1, level: 4);

        using var context = fixture.CreateContext();
        var row = await EngineFor(context).ApplyApprovalAsync("PO", transId, ApprovalAction.Rework);

        Assert.Equal(0, row.APPROVE_LEVEL);
    }

    [Fact]
    public async Task A_level_passed_with_an_action_that_ignores_it_is_an_error()
    {
        using var fixture = new SqliteFixture();
        var transId = SeedBudget(fixture, status: 1, level: 2);

        using var context = fixture.CreateContext();

        // Silently dropping it would leave the row one level up from where the
        // caller believed they had put it.
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => EngineFor(context).ApplyApprovalAsync("PO", transId, ApprovalAction.Submit, targetLevel: 3));

        Assert.Contains(nameof(ApprovalAction.Rework), ex.Message);
    }

    [Fact]
    public async Task An_undefined_action_is_refused()
    {
        using var fixture = new SqliteFixture();
        var transId = SeedBudget(fixture, status: 1, level: 2);

        using var context = fixture.CreateContext();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => EngineFor(context).ApplyApprovalAsync("PO", transId, (ApprovalAction)7));
    }

    [Fact]
    public async Task Approving_a_row_that_is_not_there_is_an_error_not_a_no_op()
    {
        using var fixture = new SqliteFixture();
        SeedBudget(fixture, status: 1, level: 2);

        using var context = fixture.CreateContext();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => EngineFor(context).ApplyApprovalAsync("PO", 9999, ApprovalAction.Approve));

        Assert.Contains("9999", ex.Message);
    }

    [Fact]
    public async Task Approving_stamps_the_audit_columns()
    {
        using var fixture = new SqliteFixture();
        var transId = SeedBudget(fixture, status: 1, level: 2);

        using (var context = fixture.CreateContext())
        {
            await EngineFor(context).ApplyApprovalAsync("PO", transId, ApprovalAction.Approve);
        }

        using (var context = fixture.CreateContext())
        {
            // The save runs through the interceptor like any other, so an
            // approval is attributable without the engine knowing about auditing.
            var saved = Assert.IsType<Budget>(
                await EngineFor(context).GetTransactionByDocTypeAsync("PO", transId));

            Assert.True(saved.IS_UPDATED);
            Assert.Equal(fixture.UserProvider.UserId, saved.UPDATED_BY);
            Assert.NotNull(saved.UPDATED_AT);
        }
    }

    [Fact]
    public async Task Unconfigured_document_type_throws_when_loading_a_transaction()
    {
        using var fixture = new SqliteFixture();
        using var context = fixture.CreateContext();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => EngineFor(context).GetTransactionByDocTypeAsync("NOPE", 1));

        Assert.Contains("NOPE", ex.Message);
    }

    [Fact]
    public async Task Blank_table_name_is_reported_rather_than_resolved()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Menus.Add(MenuFor("PO", ""));
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => EngineFor(context).GetTransactionByDocTypeAsync("PO", 1));

            Assert.Contains("TABLE_NAME", ex.Message);
        }
    }

    [Fact]
    public async Task Table_name_that_maps_to_nothing_is_reported()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Menus.Add(MenuFor("PO", "FM_NOT_MAPPED"));
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => EngineFor(context).GetTransactionByDocTypeAsync("PO", 1));

            Assert.Contains("FM_NOT_MAPPED", ex.Message);
        }
    }
}
