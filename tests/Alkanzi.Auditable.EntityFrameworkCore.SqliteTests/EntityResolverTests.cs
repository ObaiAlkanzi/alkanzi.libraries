using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Auditable.EntityFrameworkCore.SqliteTests;

public class EntityResolverTests
{
    [Fact]
    public void Resolves_entity_type_from_table_name()
    {
        using var fixture = new SqliteFixture();
        using var context = fixture.CreateContext();
        var resolver = new EntityResolver(context);

        Assert.Equal(typeof(Budget), resolver.FindEntityType("Budgets")?.ClrType);
    }

    [Fact]
    public void Table_name_matching_is_case_insensitive()
    {
        using var fixture = new SqliteFixture();
        using var context = fixture.CreateContext();
        var resolver = new EntityResolver(context);

        // Oracle hands back upper-case names; the model may spell it otherwise.
        Assert.Equal(typeof(Budget), resolver.FindEntityType("BUDGETS")?.ClrType);
    }

    [Fact]
    public void Unknown_table_returns_null_but_GetEntityType_throws()
    {
        using var fixture = new SqliteFixture();
        using var context = fixture.CreateContext();
        var resolver = new EntityResolver(context);

        Assert.Null(resolver.FindEntityType("FM_NOT_MAPPED"));

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.GetEntityType("FM_NOT_MAPPED"));
        Assert.Contains("FM_NOT_MAPPED", ex.Message);
    }

    [Fact]
    public async Task Loads_a_row_by_primary_key_without_naming_the_type()
    {
        using var fixture = new SqliteFixture();
        int id;

        using (var context = fixture.CreateContext())
        {
            var budget = new Budget { Name = "Marketing" };
            context.Budgets.Add(budget);
            context.SaveChanges();
            id = budget.Id;
        }

        using (var context = fixture.CreateContext())
        {
            var resolver = new EntityResolver(context);
            var row = await resolver.FindAsync("Budgets", id);

            Assert.Equal("Marketing", Assert.IsType<Budget>(row).Name);
        }
    }

    [Fact]
    public async Task Key_value_of_a_different_numeric_type_is_coerced()
    {
        using var fixture = new SqliteFixture();
        int id;

        using (var context = fixture.CreateContext())
        {
            var budget = new Budget { Name = "Coerced" };
            context.Budgets.Add(budget);
            context.SaveChanges();
            id = budget.Id;
        }

        using (var context = fixture.CreateContext())
        {
            var resolver = new EntityResolver(context);

            // Oracle's NUMBER surfaces as long/decimal; the key here is int.
            // Without coercion FindAsync matches on runtime type and misses.
            Assert.NotNull(await resolver.FindAsync("Budgets", (long)id));
            Assert.NotNull(await resolver.FindAsync("Budgets", (decimal)id));
        }
    }

    [Fact]
    public async Task Soft_deleted_rows_are_hidden_but_reachable_on_request()
    {
        using var fixture = new SqliteFixture();
        int id;

        using (var context = fixture.CreateContext())
        {
            var budget = new Budget { Name = "Dropped" };
            context.Budgets.Add(budget);
            context.SaveChanges();
            id = budget.Id;

            context.Budgets.Remove(budget);
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            var resolver = new EntityResolver(context);

            // EF's Find bypasses query filters, so this exclusion is the
            // resolver's own doing â€” the regression it guards against is real.
            Assert.Null(await resolver.FindAsync("Budgets", id));
        }

        using (var context = fixture.CreateContext())
        {
            var resolver = new EntityResolver(context);
            var row = await resolver.FindIncludingDeletedAsync("Budgets", id);

            Assert.True(Assert.IsType<Budget>(row).IS_DELETED);
        }
    }

    [Fact]
    public async Task Missing_row_returns_null()
    {
        using var fixture = new SqliteFixture();
        using var context = fixture.CreateContext();
        var resolver = new EntityResolver(context);

        Assert.Null(await resolver.FindAsync("Budgets", 9999));
    }

    [Fact]
    public async Task Wrong_number_of_key_values_is_reported_clearly()
    {
        using var fixture = new SqliteFixture();
        using var context = fixture.CreateContext();
        var resolver = new EntityResolver(context);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await resolver.FindAsync("Budgets", new object[] { 1, 2 }));

        Assert.Contains("1-column primary key", ex.Message);
    }

    [Fact]
    public async Task Non_auditable_entities_resolve_too()
    {
        using var fixture = new SqliteFixture();
        int id;

        using (var context = fixture.CreateContext())
        {
            var currency = new Currency { Code = "AED" };
            context.Currencies.Add(currency);
            context.SaveChanges();
            id = currency.Id;
        }

        using (var context = fixture.CreateContext())
        {
            var resolver = new EntityResolver(context);
            var row = await resolver.FindAsync("Currencies", id);

            Assert.Equal("AED", Assert.IsType<Currency>(row).Code);
        }
    }
}
