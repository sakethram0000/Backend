using MyWebApi.Models;

namespace MyWebApi.Services;

public interface ICheckerService
{
    Task<EvaluateResponse> EvaluateAsync(EvaluateRequest request);
    Task<ConfidenceScoreResponse> GetConfidenceScoreAsync(string naics, string desc);
    Task<EligibilityCheckResponse> CheckEligibilityAsync(EligibilityCheckRequest request);
    Task<RecommendationsResponse> GetRecommendationsAsync(string submissionId);
    Task<PrepareSummaryResponse> PrepareSummaryAsync(PrepareSummaryRequest request);
    Task<ResultResponse> GetResultAsync(string id);
    Task<NotifyAnalyticsResponse> NotifyAnalyticsAsync(NotifyAnalyticsRequest request);
}

public class CheckerService : ICheckerService
{
    private static readonly List<StoredSubmission> _submissions = new();

    static CheckerService()
    {
        // Seed with sample data
        _submissions.AddRange(new[]
        {
            new StoredSubmission("sub-001", "Small family restaurant", "722511", new LocationInfo("CA", "94016"), "Eligible", 0.92, "Meets appetite guidelines for restaurants in California", "Restaurant_FoodService_CA_001", DateTime.UtcNow.AddHours(-2)),
            new StoredSubmission("sub-002", "Grocery store", "445110", new LocationInfo("NY", null), "Declined", 0.85, "Product not offered for NAICS 445110 in NY", "Grocery_Retail_NY_002", DateTime.UtcNow.AddHours(-1))
        });
    }

    public async Task<EvaluateResponse> EvaluateAsync(EvaluateRequest request)
    {
        var decision = DetermineDecision(request.NaicsCode, request.Location.State);
        var matchedRule = GetMatchedRule(request.NaicsCode, request.Location.State);
        var reason = GetReason(decision, request.NaicsCode, request.Location.State);
        var confidence = CalculateConfidence(request.BusinessDesc, request.NaicsCode);

        var submission = new StoredSubmission(
            request.SubmissionId,
            request.BusinessDesc,
            request.NaicsCode,
            request.Location,
            decision,
            confidence,
            reason,
            matchedRule,
            DateTime.UtcNow
        );

        _submissions.Add(submission);

        return await Task.FromResult(new EvaluateResponse(request.SubmissionId, decision, matchedRule, reason));
    }

    public async Task<ConfidenceScoreResponse> GetConfidenceScoreAsync(string naics, string desc)
    {
        var confidence = CalculateConfidence(desc, naics);
        return await Task.FromResult(new ConfidenceScoreResponse(naics, confidence, desc));
    }

    public async Task<EligibilityCheckResponse> CheckEligibilityAsync(EligibilityCheckRequest request)
    {
        var eligible = IsEligible(request.ProductId, request.NaicsCode, request.Location.State);
        var reason = eligible 
            ? "Product is available for this NAICS code and location"
            : $"Product not offered for NAICS {request.NaicsCode} in {request.Location.State}";

        return await Task.FromResult(new EligibilityCheckResponse(request.SubmissionId, request.ProductId, eligible, reason));
    }

    public async Task<RecommendationsResponse> GetRecommendationsAsync(string submissionId)
    {
        var alternatives = new AlternativeOption[]
        {
            new("445120", "prod-102", "Convenience Store"),
            new("445220", "prod-103", "Fish Market")
        };

        return await Task.FromResult(new RecommendationsResponse(submissionId, alternatives));
    }

    public async Task<PrepareSummaryResponse> PrepareSummaryAsync(PrepareSummaryRequest request)
    {
        var confidenceLevel = request.Confidence > 0.8 ? "high" : request.Confidence > 0.6 ? "medium" : "low";
        var summary = $"This submission is {request.Decision} with {confidenceLevel} confidence. {request.Reason}";

        return await Task.FromResult(new PrepareSummaryResponse(request.SubmissionId, summary));
    }

    public async Task<ResultResponse> GetResultAsync(string id)
    {
        var submission = _submissions.FirstOrDefault(s => s.SubmissionId == id);
        if (submission == null)
            throw new KeyNotFoundException($"Submission {id} not found");

        return await Task.FromResult(new ResultResponse(submission.SubmissionId, submission.Decision, submission.Confidence, submission.Reason));
    }

    public async Task<NotifyAnalyticsResponse> NotifyAnalyticsAsync(NotifyAnalyticsRequest request)
    {
        // Simulate sending to analytics pipeline
        return await Task.FromResult(new NotifyAnalyticsResponse("ok", "Event logged to analytics", request.SubmissionId));
    }

    private string DetermineDecision(string naicsCode, string state)
    {
        return naicsCode switch
        {
            "722511" when state == "CA" => "Eligible",
            "445110" when state == "NY" => "Declined",
            "445310" => "Restricted",
            _ => "Eligible"
        };
    }

    private string GetMatchedRule(string naicsCode, string state)
    {
        return naicsCode switch
        {
            "722511" => $"Restaurant_FoodService_{state}_001",
            "445110" => $"Grocery_Retail_{state}_002",
            "445310" => $"Liquor_Retail_{state}_003",
            _ => $"General_Rule_{state}_999"
        };
    }

    private string GetReason(string decision, string naicsCode, string state)
    {
        return decision switch
        {
            "Eligible" => $"Meets appetite guidelines for NAICS {naicsCode} in {state}",
            "Declined" => $"Product not offered for NAICS {naicsCode} in {state}",
            "Restricted" => $"Limited appetite for NAICS {naicsCode} in {state}",
            _ => "Standard evaluation completed"
        };
    }

    private double CalculateConfidence(string businessDesc, string naicsCode)
    {
        var baseConfidence = 0.75;
        if (businessDesc.ToLower().Contains("restaurant") && naicsCode.StartsWith("722"))
            baseConfidence += 0.15;
        if (businessDesc.ToLower().Contains("store") && naicsCode.StartsWith("445"))
            baseConfidence += 0.12;
        
        return Math.Min(baseConfidence, 0.95);
    }

    private bool IsEligible(string productId, string naicsCode, string state)
    {
        return productId switch
        {
            "prod-101" when naicsCode == "445110" && state == "NY" => false,
            "prod-102" when naicsCode == "445120" => true,
            "prod-103" when naicsCode == "445220" => true,
            _ => true
        };
    }
}