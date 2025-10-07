namespace MyWebApi.Models;

// Evaluate models
public record EvaluateRequest(string SubmissionId, string BusinessDesc, string NaicsCode, LocationInfo Location);
public record LocationInfo(string State, string? Zipcode);
public record EvaluateResponse(string SubmissionId, string Decision, string MatchedRule, string Reason);

// Confidence score models
public record ConfidenceScoreResponse(string Naics, double ConfidenceScore, string Desc);

// Eligibility check models
public record EligibilityCheckRequest(string SubmissionId, string ProductId, string NaicsCode, LocationInfo Location);
public record EligibilityCheckResponse(string SubmissionId, string ProductId, bool Eligible, string Reason);

// Recommendations models
public record RecommendationsResponse(string SubmissionId, AlternativeOption[] Alternatives);
public record AlternativeOption(string Naics, string ProductId, string Desc);

// Summary models
public record PrepareSummaryRequest(string SubmissionId, string Decision, double Confidence, string Reason);
public record PrepareSummaryResponse(string SubmissionId, string Summary);

// Result models
public record ResultResponse(string SubmissionId, string Decision, double Confidence, string Reason);

// Analytics notification models
public record NotifyAnalyticsRequest(string SubmissionId, string Decision, int ProcessingTimeMs, DateTime Timestamp);
public record NotifyAnalyticsResponse(string Status, string Message, string SubmissionId);

// Internal storage models
public record StoredSubmission(string SubmissionId, string BusinessDesc, string NaicsCode, LocationInfo Location, string Decision, double Confidence, string Reason, string MatchedRule, DateTime EvaluatedAt);