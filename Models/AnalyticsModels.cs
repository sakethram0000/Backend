namespace MyWebApi.Models;

// Internal event storage model
public record StoredEvent(
    string EventId,
    DateTime Timestamp,
    string UserId,
    string Action,
    string? RuleId,
    string? ProductId,
    Dictionary<string, object>? Metadata
);

// POST /add models
public record AddEventRequest(
    string EventId,
    DateTime Timestamp,
    string UserId,
    string Action,
    string? RuleId,
    string? ProductId,
    Dictionary<string, object>? Metadata
);

public record AddEventResponse(
    string Status,
    string Message,
    string EventId
);

// GET /fetch models
public record EligibilityDistribution(
    int Eligible,
    int Ineligible,
    int Conditional
);

public record SubmissionOverTime(
    string Date,
    int Count
);

public record RuleByProduct(
    string ProductId,
    int RuleCount
);

public record AnalyticsMetrics(
    EligibilityDistribution EligibilityDistribution,
    List<SubmissionOverTime> SubmissionsOverTime,
    Dictionary<string, int> AppetiteShare,
    List<RuleByProduct> RulesByProduct
);

public record FetchAnalyticsResponse(
    DateTime SnapshotAt,
    AnalyticsMetrics Metrics
);