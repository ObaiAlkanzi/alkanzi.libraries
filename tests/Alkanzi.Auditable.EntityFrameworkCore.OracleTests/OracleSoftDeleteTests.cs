using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Auditable.EntityFrameworkCore.OracleTests;

/// <summary>
/// Covers what the SQLite suite structurally cannot: that the query filter
/// translates to correct Oracle SQL over a NUMBER(1) column, and that the
/// function-based unique index the README prescribes actually behaves.
/// </summary>
[Collection(OracleCollection.Name)]
public class OracleSoftDeleteTests(OracleFixture fixture)
{
    [DockerFact]
    public async Task Interceptor_stamps_against_Oracle()
    {
        await fixture.ResetAsync();

        await using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Code = "MKT", Name = "Marketing" });
            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            var budget = await context.Budgets.SingleAsync();

            Assert.Equal(42, budget.CREATED_BY);
            Assert.False(budget.IS_UPDATED);
            Assert.False(budget.IS_DELETED);
        }
    }

    [DockerFact]
    public async Task Utc_timestamp_survives_the_round_trip()
    {
        await fixture.ResetAsync();

        var before = DateTime.UtcNow;

        await using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Code = "OPS", Name = "Ops" });
            await context.SaveChangesAsync();
        }

        await using (var readBack = fixture.CreateContext())
        {
            var stored = (await readBack.Budgets.SingleAsync()).CREATED_AT;

            // Oracle's DATE type would silently truncate sub-second precision;
            // this asserts the mapping keeps the value coherent either way.
            Assert.InRange(stored, before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        }
    }

    [DockerFact]
    public async Task Query_filter_hides_soft_deleted_rows_on_Oracle()
    {
        await fixture.ResetAsync();

        await using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Code = "KEEP", Name = "Keep" });
            context.Budgets.Add(new Budget { Code = "DROP", Name = "Drop" });
            await context.SaveChangesAsync();

            context.Budgets.Remove(await context.Budgets.SingleAsync(b => b.Code == "DROP"));
            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            Assert.Equal("KEEP", (await context.Budgets.SingleAsync()).Code);
            Assert.Equal(2, await context.Budgets.IgnoreQueryFilters().CountAsync());
        }
    }

    [DockerFact]
    public async Task Rows_with_a_null_IS_DELETED_stay_visible_on_Oracle()
    {
        await fixture.ResetAsync();

        await using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Code = "LEGACY", Name = "Legacy" });
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlRawAsync("UPDATE BUDGETS SET IS_DELETED = NULL");
        }

        // The point of `!= true` over `== false`: Oracle three-valued logic must
        // not swallow rows predating the audit columns.
        await using (var context = fixture.CreateContext())
        {
            Assert.Single(await context.Budgets.ToListAsync());
        }
    }

    [DockerFact]
    public async Task Code_can_be_reused_after_the_holder_is_soft_deleted()
    {
        await fixture.ResetAsync();

        await using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Code = "SHARED", Name = "First" });
            await context.SaveChangesAsync();

            context.Budgets.Remove(await context.Budgets.SingleAsync());
            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            context.Budgets.Add(new Budget { Code = "SHARED", Name = "Second" });

            // Without the function-based index this throws ORA-00001.
            await context.SaveChangesAsync();

            Assert.Equal("Second", (await context.Budgets.SingleAsync()).Name);
        }
    }

    [DockerFact]
    public async Task Two_live_rows_still_cannot_share_a_code()
    {
        await fixture.ResetAsync();

        await using var context = fixture.CreateContext();
        context.Budgets.Add(new Budget { Code = "UNIQUE", Name = "First" });
        await context.SaveChangesAsync();

        context.Budgets.Add(new Budget { Code = "UNIQUE", Name = "Second" });

        // The index must not have been weakened into uselessness by the CASE.
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
