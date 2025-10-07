using MyWebApi.Models;

namespace MyWebApi.Services;

public interface ISearchService
{
    Task<SearchRuleResponse> GetRulesAsync(int page, int pageSize, string? sortBy, string? q);
    Task<SearchRuleResponse> GetRulesByKeywordAsync(string keyword, int page, int pageSize);
    Task<SearchRuleResponse> GetRulesByNaicsAsync(string naicsCode, int page, int pageSize);
    Task<SearchRuleResponse> GetRulesByBusinessTypeAsync(string type, int page, int pageSize);
    Task<SearchRuleResponse> GetRulesByCustomFilterAsync(CustomFilterRequest request);
}

public class SearchService : ISearchService
{
    private static readonly List<StoredSearchRule> _rules = new()
    {
        new("rul-1001", "No liquor stores in Zone A", "Blocks appetite for retail liquor locations in high-risk zones.", "Retail", new[] { "445310" }, new[] { "CA", "TX" }, "Acme Insurance", "General Liability", new[] { "no-drive-thru", "no-24hr" }, "high", DateTime.Parse("2025-06-15T09:12:00Z"), DateTime.Parse("2025-09-01T14:22:00Z")),
        new("rul-1002", "High-risk restaurants - extra premium", "Restaurants with >100 seats require special underwriting.", "Restaurant", new[] { "722511" }, new[] { "NY" }, "Beta Mutual", "Property & Liability", new[] { "seating-limit" }, "medium", DateTime.Parse("2024-11-05T11:00:00Z"), DateTime.Parse("2025-02-20T08:30:00Z")),
        new("rul-1010", "Outdoor dining - location sensitivity", "Outdoor dining near busy highways flagged for extra review.", "Restaurant", new[] { "722511", "722513" }, new[] { "CA", "NV" }, "Acme Insurance", "General Liability", new[] { "no-sidewalk-dining" }, "low", DateTime.Parse("2025-01-10T10:00:00Z"), DateTime.Parse("2025-03-15T09:45:00Z")),
        new("rul-1003", "Property coverage restrictions", "Limits property coverage in flood zones.", "Property", new[] { "445110" }, new[] { "FL", "LA" }, "Beta Mutual", "Property", new[] { "flood-zone-restricted" }, "high", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-5)),
        new("rul-1004", "Workers comp exclusions", "Excludes certain high-risk activities.", "Services", new[] { "561720" }, new[] { "CA", "NY" }, "Beta Mutual", "Workers Comp", new[] { "no-hazardous-materials" }, "low", DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(-1))
    };

    public async Task<SearchRuleResponse> GetRulesAsync(int page, int pageSize, string? sortBy, string? q)
    {
        var filteredRules = _rules.AsQueryable();

        if (!string.IsNullOrEmpty(q))
        {
            var query = q.ToLower();
            filteredRules = filteredRules.Where(r => 
                r.Title.ToLower().Contains(query) ||
                r.Description.ToLower().Contains(query) ||
                r.BusinessType.ToLower().Contains(query) ||
                r.Restrictions.Any(res => res.ToLower().Contains(query))
            );
        }

        filteredRules = ApplySorting(filteredRules, sortBy);

        var totalItems = filteredRules.Count();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var rules = filteredRules
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToSearchRule)
            .ToArray();

        var pagination = new PaginationInfo(page, pageSize, totalPages, totalItems);
        return await Task.FromResult(new SearchRuleResponse(rules, pagination));
    }

    public async Task<SearchRuleResponse> GetRulesByKeywordAsync(string keyword, int page, int pageSize)
    {
        var query = keyword.ToLower();
        var filteredRules = _rules.Where(r =>
            r.Title.ToLower().Contains(query) ||
            r.Description.ToLower().Contains(query) ||
            r.BusinessType.ToLower().Contains(query) ||
            r.States.Any(s => s.ToLower().Contains(query)) ||
            r.Restrictions.Any(res => res.ToLower().Contains(query))
        );

        var totalItems = filteredRules.Count();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var rules = filteredRules
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToSearchRule)
            .ToArray();

        var pagination = new PaginationInfo(page, pageSize, totalPages, totalItems);
        return await Task.FromResult(new SearchRuleResponse(rules, pagination));
    }

    public async Task<SearchRuleResponse> GetRulesByNaicsAsync(string naicsCode, int page, int pageSize)
    {
        var filteredRules = _rules.Where(r => r.NaicsCodes.Contains(naicsCode));

        var totalItems = filteredRules.Count();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var rules = filteredRules
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToSearchRule)
            .ToArray();

        var pagination = new PaginationInfo(page, pageSize, totalPages, totalItems);
        return await Task.FromResult(new SearchRuleResponse(rules, pagination));
    }

    public async Task<SearchRuleResponse> GetRulesByBusinessTypeAsync(string type, int page, int pageSize)
    {
        var filteredRules = _rules.Where(r => r.BusinessType.Equals(type, StringComparison.OrdinalIgnoreCase));

        var totalItems = filteredRules.Count();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var rules = filteredRules
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToSearchRule)
            .ToArray();

        var pagination = new PaginationInfo(page, pageSize, totalPages, totalItems);
        return await Task.FromResult(new SearchRuleResponse(rules, pagination));
    }

    public async Task<SearchRuleResponse> GetRulesByCustomFilterAsync(CustomFilterRequest request)
    {
        var filteredRules = _rules.AsQueryable();

        if (!string.IsNullOrEmpty(request.Carrier))
            filteredRules = filteredRules.Where(r => r.Carrier == request.Carrier);

        if (!string.IsNullOrEmpty(request.Product))
            filteredRules = filteredRules.Where(r => r.Product == request.Product);

        if (request.States?.Length > 0)
            filteredRules = filteredRules.Where(r => r.States.Any(s => request.States.Contains(s)));

        if (request.NaicsCodes?.Length > 0)
            filteredRules = filteredRules.Where(r => r.NaicsCodes.Any(n => request.NaicsCodes.Contains(n)));

        if (request.BusinessTypes?.Length > 0)
            filteredRules = filteredRules.Where(r => request.BusinessTypes.Contains(r.BusinessType));

        if (!string.IsNullOrEmpty(request.Priority))
            filteredRules = filteredRules.Where(r => r.Priority == request.Priority);

        if (!request.IncludeRestricted)
            filteredRules = filteredRules.Where(r => r.Restrictions.Length == 0);

        filteredRules = ApplySorting(filteredRules, request.SortBy);

        var totalItems = filteredRules.Count();
        var totalPages = (int)Math.Ceiling((double)totalItems / request.PageSize);

        var rules = filteredRules
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(MapToSearchRule)
            .ToArray();

        var pagination = new PaginationInfo(request.Page, request.PageSize, totalPages, totalItems);
        return await Task.FromResult(new SearchRuleResponse(rules, pagination));
    }

    private IQueryable<StoredSearchRule> ApplySorting(IQueryable<StoredSearchRule> rules, string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy))
            return rules.OrderBy(r => r.RuleId);

        var sortParts = sortBy.Split(',');
        var orderedRules = rules.AsQueryable();

        foreach (var part in sortParts)
        {
            var sortField = part.Split(':');
            var field = sortField[0];
            var direction = sortField.Length > 1 ? sortField[1] : "asc";

            orderedRules = field.ToLower() switch
            {
                "priority" => direction == "desc" 
                    ? orderedRules.OrderByDescending(r => r.Priority == "high" ? 3 : r.Priority == "medium" ? 2 : 1)
                    : orderedRules.OrderBy(r => r.Priority == "high" ? 3 : r.Priority == "medium" ? 2 : 1),
                "createdat" => direction == "desc" 
                    ? orderedRules.OrderByDescending(r => r.CreatedAt)
                    : orderedRules.OrderBy(r => r.CreatedAt),
                _ => orderedRules.OrderBy(r => r.RuleId)
            };
        }

        return orderedRules;
    }

    private SearchRule MapToSearchRule(StoredSearchRule stored)
    {
        return new SearchRule(
            stored.RuleId,
            stored.Title,
            stored.Description,
            stored.BusinessType,
            stored.NaicsCodes,
            stored.States,
            stored.Carrier,
            stored.Product,
            stored.Restrictions,
            stored.Priority,
            stored.CreatedAt,
            stored.UpdatedAt
        );
    }
}