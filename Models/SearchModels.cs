namespace MyWebApi.Models;

// Search rule models
public record SearchRuleResponse(SearchRule[] Data, PaginationInfo Pagination);

public record SearchRule(
    string RuleId,
    string Title,
    string Description,
    string BusinessType,
    string[] NaicsCodes,
    string[] States,
    string Carrier,
    string Product,
    string[] Restrictions,
    string Priority,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// Custom filter models
public record CustomFilterRequest(
    string? Carrier,
    string? Product,
    string[]? States,
    string[]? NaicsCodes,
    string[]? BusinessTypes,
    string? Priority,
    bool IncludeRestricted,
    int Page,
    int PageSize,
    string? SortBy
);

// Internal storage model
public record StoredSearchRule(
    string RuleId,
    string Title,
    string Description,
    string BusinessType,
    string[] NaicsCodes,
    string[] States,
    string Carrier,
    string Product,
    string[] Restrictions,
    string Priority,
    DateTime CreatedAt,
    DateTime UpdatedAt
);