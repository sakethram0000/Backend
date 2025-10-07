namespace MyWebApi.Models;

// Authentication models
public record LoginRequest(string Username, string Password);
public record LoginResponse(string AccessToken, string TokenType, int ExpiresIn, UserInfo User);
public record UserInfo(string Id, string Name, string[] Roles, bool IsActive, DateTime? LastLoginAt, string? AuthProvider);

// Registration models
public record RegisterRequest(string OrganizationType, string OrganizationName, AdminInfo Admin);
public record AdminInfo(string Name, string Email, string Phone);
public record RegisterResponse(string UserId, string Status, string Message);

// User models
public record UserProfile(string Id, string Name, string Email, string[] Roles, OrganizationInfo Organization, DateTime CreatedAt, bool IsActive, DateTime? LastLoginAt, string? AuthProvider);
public record OrganizationInfo(string Id, string Name);
public record UsersResponse(UserSummary[] Data, PaginationInfo Pagination);
public record UserSummary(string Id, string Name, string Email, string[] Roles, bool IsActive);

// Product models
public record ProductDetails(string Id, string Name, string? Description, string? ProductType, string Carrier, int PerOccurrence, int Aggregate, int MinAnnualRevenue, int MaxAnnualRevenue, string NaicsAllowed, DateTime CreatedAt);
public record ProductLimits(int PerOccurrence, int Aggregate);
public record ProductEligibility(int MinAnnualRevenue, int MaxAnnualRevenue);
public record ProductsResponse(ProductSummary[] Data, PaginationInfo Pagination);
public record ProductSummary(string Id, string Name, string Carrier);

// Rule models
public record RuleDetails(string RuleId, string Title, string? Description, string? BusinessType, string[]? NaicsCodes, string[]? States, string? Carrier, string? Product, string[]? Restrictions, string? Priority, string? Outcome, string? RuleVersion, string? Status, DateTime? EffectiveFrom, DateTime? EffectiveTo, decimal? MinRevenue, decimal? MaxRevenue, int? MinYearsInBusiness, int? MaxYearsInBusiness, int? PriorClaimsAllowed, string[]? Conditions, string? ContactEmail, string? CreatedBy, DateTime CreatedAt, DateTime? UpdatedAt, string? AdditionalJson);
public record RulesResponse(RuleSummary[] Data, PaginationInfo Pagination);
public record RuleSummary(string RuleId, string Title, string? Priority, string? Status);

// Rule upload models
public record RuleUploadResponse(string UploadId, string Status, int Created, int Updated, int Failed, UploadError[] Errors, string ReportUrl);
public record UploadError(int Row, string Error);

// Canvas analytics models
public record CanvasAnalyticsResponse(DateTime SnapshotAt, CanvasMetrics Metrics, string PowerBIEmbedUrl);
public record CanvasMetrics(int TotalRules, Dictionary<string, int> RulesByPriority, Dictionary<string, int> ProductsByCarrier, int RecentUploads, int TotalUsers, int TotalCarriers, int TotalProducts, Dictionary<string, int> UsersByRole, RealGrowthData[] GrowthData);
public record RealGrowthData(string Date, int Users, int Rules, int Carriers);

// Carrier models
public record CarrierDetails(string CarrierId, string LegalName, string DisplayName, string? Country, string? HeadquartersAddress, string? PrimaryContactName, string? PrimaryContactEmail, string? PrimaryContactPhone, string? TechnicalContactName, string? TechnicalContactEmail, string? AuthMethod, string? SsoMetadataUrl, string? ApiClientId, string? ApiSecretKeyRef, string? DataResidency, string[]? ProductsOffered, bool RuleUploadAllowed, string? RuleUploadMethod, bool RuleApprovalRequired, bool DefaultRuleVersioning, bool UseNaicsEnrichment, string? PreferredNaicsSource, string? PasWebhookUrl, string? WebhookAuthType, string? WebhookSecretRef, string? ContractRef, string? BillingContactEmail, int? RetentionPolicyDays, string? CreatedBy, DateTime CreatedAt, DateTime? UpdatedAt, string? AdditionalJson);
public record CarriersResponse(CarrierSummary[] Data, PaginationInfo Pagination);
public record CarrierSummary(string CarrierId, string LegalName, string DisplayName, string? Country, string? PrimaryContactEmail);

// Common models
public record PaginationInfo(int Page, int PageSize, int TotalPages, int TotalItems);

// Quick user creation
public record CreateUserRequest(string Name, string Email, string Role, string OrganizationName);
public record CreateUserResponse(string UserId, string Email, string TemporaryPassword, string Message);

// Internal storage models
public record StoredUser(string Id, string Name, string Email, string[] Roles, string OrganizationId, string OrganizationName, DateTime CreatedAt, bool IsActive, DateTime? LastLoginAt, string? AuthProvider);
public record StoredProduct(string Id, string Name, string? Description, string? ProductType, string Carrier, int PerOccurrence, int Aggregate, int MinAnnualRevenue, int MaxAnnualRevenue, string NaicsAllowed, DateTime CreatedAt);