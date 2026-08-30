
using Alkanzi.Erp.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Erp.DataAccess.Search;

public sealed record SearchHit(
    string EntityType,
    string Label,
    long Id,
    int? DocNum,
    string Title,
    string? Subtitle,
    int BranchId,
    float Rank);

public sealed record SearchResult(int Total, IReadOnlyList<SearchHit> Hits);

/// <summary>
/// Omni-search over the unified index, using PostgreSQL full-text search.
/// </summary>
public sealed class SearchService
{
    private readonly ErpDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SearchService(ErpDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SearchResult> SearchAsync(
        string? term, string? type, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        term = (term ?? "").Trim();
        if (term.Length < 2) return new SearchResult(0, Array.Empty<SearchHit>());

        skip = Math.Max(0, skip);
        take = take is <= 0 or > 100 ? 20 : take;

        // Prefix matching, so the dropdown responds while the user is still typing:
        // "trad" has to hit "Trading". Each token gets ':*' and they are AND-ed, which is
        // what ToTsQuery over "a:* & b:*" gives. EF.Functions.WebSearchToTsQuery would be
        // friendlier for quoted phrases but cannot express a prefix.
        var tsQuery = string.Join(" & ",
            term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => Sanitise(t) + ":*")
                .Where(t => t.Length > 2));

        if (tsQuery.Length == 0) return new SearchResult(0, Array.Empty<SearchHit>());

        // Tenant scoping, and it is not optional. The index is one table spanning every
        // company, so without this filter a signed-in user searching "trading" would be
        // served another company's vendors and purchase orders. Applied first so it also
        // narrows the count below.
        //
        // An unauthenticated or company-less caller gets nothing rather than everything:
        // failing closed is the only safe default when the scope cannot be established.
        var companyId = _currentUser.CompanyId;
        if (companyId <= 0) return new SearchResult(0, Array.Empty<SearchHit>());

        var q = _db.SearchDocuments.AsNoTracking()
            .Where(d => d.CompanyId == companyId)
            .Where(d => d.SearchVector.Matches(EF.Functions.ToTsQuery("simple", tsQuery)));

        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(d => d.EntityType == type);

        // A real COUNT, not the number of rows the page happened to return: a pager that
        // reports the size of its own page is worse than no pager at all.
        var total = await q.CountAsync(ct).ConfigureAwait(false);

        var hits = await q
            .Select(d => new
            {
                d.EntityType, d.Label, d.EntityId, d.DocNum, d.Title, d.Subtitle, d.BranchId,
                Rank = d.SearchVector.Rank(EF.Functions.ToTsQuery("simple", tsQuery))
            })
            // Id as the final tiebreak so paging is a stable slice of one ordering —
            // without it, equal ranks can reshuffle between pages and rows repeat.
            .OrderByDescending(x => x.Rank)
            .ThenByDescending(x => x.EntityId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new SearchResult(total, hits
            .Select(h => new SearchHit(h.EntityType, h.Label, h.EntityId, h.DocNum, h.Title, h.Subtitle, h.BranchId, h.Rank))
            .ToList());
    }

    /// <summary>
    /// Strips the characters tsquery treats as operators. The term is interpolated into a
    /// tsquery string, so it must not be able to carry <c>&amp; | ! ( ) :</c> through — that
    /// would be a syntax error at best and an injected operator at worst.
    /// </summary>
    private static string Sanitise(string token)
    {
        var clean = new string(token.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
        return clean;
    }
}
