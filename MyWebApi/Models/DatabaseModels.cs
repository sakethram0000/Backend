using System.ComponentModel.DataAnnotations;

namespace MyWebApi.Models;

// Database entity models with parameterless constructors
public class DbUser
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Roles { get; set; }
    public string? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? AuthProvider { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }
}

public class DbProduct
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public int PerOccurrence { get; set; }
    public int Aggregate { get; set; }
    public int MinAnnualRevenue { get; set; }
    public int MaxAnnualRevenue { get; set; }
    public string NaicsAllowed { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DbRule
{
    [Key]
    public string RuleId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BusinessType { get; set; }
    public string? NaicsCodes { get; set; }
    public string? States { get; set; }
    public string? Carrier { get; set; }
    public string? Product { get; set; }
    public string? Restrictions { get; set; }
    public string? Priority { get; set; }
    public string? Outcome { get; set; }
    public string? RuleVersion { get; set; }
    public string? Status { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public decimal? MinRevenue { get; set; }
    public decimal? MaxRevenue { get; set; }
    public int? MinYearsInBusiness { get; set; }
    public int? MaxYearsInBusiness { get; set; }
    public int? PriorClaimsAllowed { get; set; }
    public string? Conditions { get; set; }
    public string? ContactEmail { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? AdditionalJson { get; set; }
}

public class DbEvent
{
    [Key]
    public string EventId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? RuleId { get; set; }
    public string? ProductId { get; set; }
    public string? Metadata { get; set; }
}

public class DbCarrier
{
    [Key]
    public string CarrierId { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? HeadquartersAddress { get; set; }
    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactPhone { get; set; }
    public string? TechnicalContactName { get; set; }
    public string? TechnicalContactEmail { get; set; }
    public string? AuthMethod { get; set; }
    public string? SsoMetadataUrl { get; set; }
    public string? ApiClientId { get; set; }
    public string? ApiSecretKeyRef { get; set; }
    public string? DataResidency { get; set; }
    public string? ProductsOffered { get; set; }
    public bool RuleUploadAllowed { get; set; } = false;
    public string? RuleUploadMethod { get; set; }
    public bool RuleApprovalRequired { get; set; } = true;
    public bool DefaultRuleVersioning { get; set; } = true;
    public bool UseNaicsEnrichment { get; set; } = false;
    public string? PreferredNaicsSource { get; set; }
    public string? PasWebhookUrl { get; set; }
    public string? WebhookAuthType { get; set; }
    public string? WebhookSecretRef { get; set; }
    public string? ContractRef { get; set; }
    public string? BillingContactEmail { get; set; }
    public int? RetentionPolicyDays { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? AdditionalJson { get; set; }
}

public class DbSubmission
{
    [Key]
    public string SubmissionId { get; set; } = string.Empty;
    public string BusinessDesc { get; set; } = string.Empty;
    public string NaicsCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? Zipcode { get; set; }
    public string Decision { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string MatchedRule { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; }
}