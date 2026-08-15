using Alkanzi.SearchEngine;

namespace Alkanzi.SearchEngine.Tests;

public class SearchEngineTests
{
    private sealed class FakeProvider : ISearchProvider
    {
        private readonly IReadOnlyList<SearchHit> _hits;
        private readonly bool _throws;
        public FakeProvider(string type, IReadOnlyList<SearchHit> hits, bool throws = false)
        { EntityType = type; _hits = hits; _throws = throws; }
        public string EntityType { get; }
        public Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery q, SearchScope s, CancellationToken ct = default)
            => _throws ? throw new InvalidOperationException("boom") : Task.FromResult(_hits);
    }

    private static SearchHit Hit(string type, long id, double score = 1, int branch = 0)
        => new() { EntityType = type, Id = id, Title = $"{type}-{id}", Score = score, BranchId = branch };

    [Fact]
    public async Task Empty_term_returns_empty()
    {
        var engine = new SearchEngine(new[] { new FakeProvider("a", new[] { Hit("a", 1) }) });
        var r = await engine.SearchAsync(new SearchQuery { Term = "  " }, SearchScope.All);
        Assert.Empty(r.Hits);
        Assert.Equal(0, r.Total);
    }

    [Fact]
    public async Task Merges_and_ranks_by_score_desc()
    {
        var engine = new SearchEngine(new ISearchProvider[]
        {
            new FakeProvider("a", new[] { Hit("a", 1, score: 1) }),
            new FakeProvider("b", new[] { Hit("b", 2, score: 2) }),
        });
        var r = await engine.SearchAsync(new SearchQuery { Term = "x" }, SearchScope.All);
        Assert.Equal(2, r.Total);
        Assert.Equal("b", r.Hits[0].EntityType);   // higher score first
        Assert.Equal("a", r.Hits[1].EntityType);
    }

    [Fact]
    public async Task Query_type_filter_limits_providers()
    {
        var engine = new SearchEngine(new ISearchProvider[]
        {
            new FakeProvider("a", new[] { Hit("a", 1) }),
            new FakeProvider("b", new[] { Hit("b", 2) }),
        });
        var r = await engine.SearchAsync(new SearchQuery { Term = "x", Types = new[] { "b" } }, SearchScope.All);
        Assert.Single(r.Hits);
        Assert.Equal("b", r.Hits[0].EntityType);
    }

    [Fact]
    public async Task Scope_allowed_types_and_branches_filter_results()
    {
        var engine = new SearchEngine(new ISearchProvider[]
        {
            new FakeProvider("a", new[] { Hit("a", 1, branch: 5) }),
            new FakeProvider("b", new[] { Hit("b", 2, branch: 9) }),
        });
        var scope = new SearchScope { AllowedTypes = new[] { "a", "b" }, AllowedBranches = new[] { 5 } };
        var r = await engine.SearchAsync(new SearchQuery { Term = "x" }, scope);
        Assert.Single(r.Hits);
        Assert.Equal(5, r.Hits[0].BranchId);
    }

    [Fact]
    public async Task Failing_provider_is_skipped_not_fatal()
    {
        var engine = new SearchEngine(new ISearchProvider[]
        {
            new FakeProvider("a", new[] { Hit("a", 1) }),
            new FakeProvider("bad", Array.Empty<SearchHit>(), throws: true),
        });
        var r = await engine.SearchAsync(new SearchQuery { Term = "x" }, SearchScope.All);
        Assert.Single(r.Hits);
        Assert.Equal("a", r.Hits[0].EntityType);
    }

    [Fact]
    public async Task Paging_applies_after_merge()
    {
        var hits = Enumerable.Range(1, 10).Select(i => Hit("a", i, score: i)).ToArray();
        var engine = new SearchEngine(new[] { new FakeProvider("a", hits) });
        var r = await engine.SearchAsync(new SearchQuery { Term = "x", Skip = 2, Take = 3 }, SearchScope.All);
        Assert.Equal(10, r.Total);
        Assert.Equal(3, r.Hits.Count);
        Assert.Equal(8, r.Hits[0].Id);   // scores 10,9 skipped -> starts at 8
    }
}
