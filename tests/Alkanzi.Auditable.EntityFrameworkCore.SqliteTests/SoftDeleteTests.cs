using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Auditable.EntityFrameworkCore.Tests;

public class SoftDeleteTests
{
    [Fact]
    public void Removing_an_entity_stamps_it_and_keeps_the_row()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Name = "Marketing" });
            context.SaveChanges();
        }

        fixture.UserProvider.UserId = 3;

        using (var context = fixture.CreateContext())
        {
            context.Budgets.Remove(context.Budgets.Single());
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            var deleted = context.Budgets.IgnoreQueryFilters().Single();

            Assert.True(deleted.IS_DELETED);
            Assert.Equal(3, deleted.DELETED_BY);
            Assert.NotNull(deleted.DELETED_AT);
        }
    }

    [Fact]
    public void Soft_deleted_rows_are_hidden_by_the_global_query_filter()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Name = "Keep" });
            context.Budgets.Add(new Budget { Name = "Drop" });
            context.SaveChanges();

            context.Budgets.Remove(context.Budgets.Single(b => b.Name == "Drop"));
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            Assert.Equal("Keep", context.Budgets.Single().Name);
            Assert.Equal(2, context.Budgets.IgnoreQueryFilters().Count());
        }
    }

    [Fact]
    public void Rows_with_a_null_IS_DELETED_are_still_visible()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Name = "Legacy" });
            context.SaveChanges();
        }

        // Simulates a pre-existing row written before auditing was introduced.
        using (var context = fixture.CreateContext())
        {
            context.Database.ExecuteSqlRaw("UPDATE Budgets SET IS_DELETED = NULL");
        }

        using (var context = fixture.CreateContext())
        {
            Assert.Single(context.Budgets);
        }
    }

    [Fact]
    public void Pending_edits_survive_a_soft_delete_in_the_same_unit_of_work()
    {
        using var fixture = new SqliteFixture();

        using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Name = "Marketing", Amount = 100m });
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            var budget = context.Budgets.Single();
            budget.Amount = 999m;
            context.Budgets.Remove(budget);
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            var deleted = context.Budgets.IgnoreQueryFilters().Single();

            Assert.True(deleted.IS_DELETED);
            Assert.Equal(999m, deleted.Amount);
        }
    }

    [Fact]
    public void Soft_delete_can_be_turned_off()
    {
        using var fixture = new SqliteFixture();
        var hardDelete = new AuditableOptions { SoftDelete = false };

        using (var context = fixture.CreateContext(hardDelete))
        {
            context.Budgets.Add(new Budget { Name = "Marketing" });
            context.SaveChanges();

            context.Budgets.Remove(context.Budgets.Single());
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext(hardDelete))
        {
            Assert.Empty(context.Budgets.IgnoreQueryFilters());
        }
    }
}
