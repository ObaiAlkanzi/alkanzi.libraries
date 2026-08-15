using Alkanzi.SearchEngine;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.SearchEngine.Tests;

public class TransactionSearchProviderTests : IDisposable
{
    // Stand-in for a TRANSACTION_BASE entity.
    private sealed class FakeTxn : ISearchableTransaction
    {
        public int ID { get; set; }
        public int DOC_NUM { get; set; }
        public string DOC_TYPE { get; set; } = "";
        public DateTime DOC_DATE { get; set; }
        public int BRANCH_ID { get; set; }
        public int DOC_STATUS { get; set; }
        public int APPROVE_STATUS { get; set; }
        public bool IS_DELETED { get; set; }
    }

    private sealed class TestDb : DbContext
    {
        public TestDb(DbContextOptions options) : base(options) { }
        public DbSet<FakeTxn> Txns => Set<FakeTxn>();
        protected override void OnModelCreating(ModelBuilder b) => b.Entity<FakeTxn>().HasKey(x => x.ID);
    }

    private readonly SqliteConnection _conn;
    private readonly TestDb _db;

    public TransactionSearchProviderTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _db = new TestDb(new DbContextOptionsBuilder().UseSqlite(_conn).Options);
        _db.Database.EnsureCreated();
        _db.AddRange(
            new FakeTxn { ID = 100, DOC_NUM = 500, BRANCH_ID = 1, DOC_DATE = new DateTime(2026, 1, 1) },
            new FakeTxn { ID = 200, DOC_NUM = 100, BRANCH_ID = 1, IS_DELETED = true },           // soft-deleted
            new FakeTxn { ID = 300, DOC_NUM = 999, BRANCH_ID = 7, DOC_DATE = new DateTime(2026, 2, 1) });
        _db.SaveChanges();
    }

    private TransactionSearchProvider<FakeTxn> Provider()
        => new("txn", () => _db.Txns, title: x => $"T-{x.ID}");

    [Fact]
    public async Task Matches_by_id_and_skips_soft_deleted()
    {
        var r = await Provider().SearchAsync(new SearchQuery { Term = "100" }, SearchScope.All);
        Assert.Single(r);                       // id 100 matches; id 200 (DOC_NUM 100) is soft-deleted
        Assert.Equal(100, r[0].Id);
        Assert.Equal("T-100", r[0].Title);
        Assert.Equal(2.0, r[0].Score);          // exact-id beats doc-number
    }

    [Fact]
    public async Task Matches_by_document_number()
    {
        var r = await Provider().SearchAsync(new SearchQuery { Term = "999" }, SearchScope.All);
        Assert.Single(r);
        Assert.Equal(300, r[0].Id);
        Assert.Equal(1.0, r[0].Score);          // doc-number match
    }

    [Fact]
    public async Task Branch_scope_filters()
    {
        var scope = new SearchScope { AllowedBranches = new[] { 1 } };
        var r = await Provider().SearchAsync(new SearchQuery { Term = "999" }, scope);
        Assert.Empty(r);                        // id 300 is branch 7, not allowed
    }

    [Fact]
    public async Task Non_numeric_term_returns_empty()
    {
        var r = await Provider().SearchAsync(new SearchQuery { Term = "acme" }, SearchScope.All);
        Assert.Empty(r);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
