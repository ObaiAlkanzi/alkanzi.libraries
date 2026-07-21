using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Auditable.EntityFrameworkCore.Tests;

public class AuditableInterceptorTests
{
    [Fact]
    public void Adding_an_entity_stamps_created_metadata()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Name = "Marketing", Amount = 1000m });
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            var budget = context.Budgets.Single();

            Assert.Equal(42, budget.CREATED_BY);
            Assert.False(budget.IS_UPDATED);
            Assert.False(budget.IS_DELETED);
            Assert.Null(budget.UPDATED_AT);
            Assert.NotEqual(default, budget.CREATED_AT);
        }
    }

    [Fact]
    public void Created_timestamp_is_utc()
    {
        using var fixture = new SqliteFixture();
        using var context = fixture.CreateContext();

        var budget = new Budget { Name = "Ops" };
        context.Budgets.Add(budget);
        context.SaveChanges();

        // Within a minute of UtcNow — would fail on a non-UTC host if the
        // library ever regressed to DateTime.Now.
        Assert.True((DateTime.UtcNow - budget.CREATED_AT).Duration() < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Modifying_an_entity_stamps_updated_metadata()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Name = "Marketing", Amount = 1000m });
            context.SaveChanges();
        }

        fixture.UserProvider.UserId = 7;

        using (var context = fixture.CreateContext())
        {
            var budget = context.Budgets.Single();
            budget.Amount = 2000m;
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            var budget = context.Budgets.Single();

            Assert.True(budget.IS_UPDATED);
            Assert.Equal(7, budget.UPDATED_BY);
            Assert.NotNull(budget.UPDATED_AT);
            Assert.Equal(42, budget.CREATED_BY); // creator is not overwritten
        }
    }

    [Fact]
    public async Task SaveChangesAsync_stamps_the_same_way()
    {
        using var fixture = new SqliteFixture();
        await using var context = fixture.CreateContext();

        context.Budgets.Add(new Budget { Name = "Async" });
        await context.SaveChangesAsync();

        Assert.Equal(42, context.Budgets.Single().CREATED_BY);
    }

    [Fact]
    public void Null_user_falls_back_to_the_configured_system_id()
    {
        using var fixture = new SqliteFixture();
        fixture.UserProvider.UserId = null;

        using var context = fixture.CreateContext(new AuditableOptions { SystemUserId = -1 });
        context.Budgets.Add(new Budget { Name = "Seeded" });
        context.SaveChanges();

        Assert.Equal(-1, context.Budgets.Single().CREATED_BY);
    }

    [Fact]
    public void Entities_that_are_not_auditable_are_left_alone()
    {
        using var fixture = new SqliteFixture();
        using var context = fixture.CreateContext();

        context.Currencies.Add(new Currency { Code = "AED" });
        context.SaveChanges();

        context.Currencies.Remove(context.Currencies.Single());
        context.SaveChanges();

        Assert.Empty(context.Currencies); // hard delete, as EF would normally do
    }
}
