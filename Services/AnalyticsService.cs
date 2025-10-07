using MyWebApi.Models;

namespace MyWebApi.Services;

public interface IAnalyticsService
{
    Task<AddEventResponse> AddEventAsync(AddEventRequest request);
    Task<FetchAnalyticsResponse> FetchAnalyticsAsync(DateTime? since, DateTime? until);
}

public class AnalyticsService : IAnalyticsService
{
    private static readonly List<StoredEvent> _events = new();

    static AnalyticsService()
    {
        // Seed with sample data
        var sampleEvents = new List<StoredEvent>
        {
            new("evt-1001", DateTime.UtcNow.AddDays(-5), "usr-100", "rule_view", "rul-1001", "prod-200", new Dictionary<string, object> { {"device", "web"}, {"location", "CA"} }),
            new("evt-1002", DateTime.UtcNow.AddDays(-4), "usr-101", "submission", "rul-1002", "prod-201", new Dictionary<string, object> { {"device", "mobile"}, {"location", "TX"} }),
            new("evt-1003", DateTime.UtcNow.AddDays(-3), "usr-102", "rule_view", "rul-1003", "prod-200", new Dictionary<string, object> { {"device", "web"}, {"location", "NY"} }),
            new("evt-1004", DateTime.UtcNow.AddDays(-2), "usr-103", "submission", "rul-1001", "prod-202", new Dictionary<string, object> { {"device", "web"}, {"location", "FL"} }),
            new("evt-1005", DateTime.UtcNow.AddDays(-1), "usr-104", "rule_view", "rul-1004", "prod-201", new Dictionary<string, object> { {"device", "tablet"}, {"location", "OH"} })
        };
        _events.AddRange(sampleEvents);
    }

    public async Task<AddEventResponse> AddEventAsync(AddEventRequest request)
    {
        try
        {
            var storedEvent = new StoredEvent(
                request.EventId,
                request.Timestamp,
                request.UserId,
                request.Action,
                request.RuleId,
                request.ProductId,
                request.Metadata
            );
            
            _events.Add(storedEvent);
            return await Task.FromResult(new AddEventResponse("success", "Event recorded", request.EventId));
        }
        catch (Exception ex)
        {
            return await Task.FromResult(new AddEventResponse("error", $"Failed to record event: {ex.Message}", request.EventId));
        }
    }

    public async Task<FetchAnalyticsResponse> FetchAnalyticsAsync(DateTime? since, DateTime? until)
    {
        var filteredEvents = _events.AsQueryable();
        
        if (since.HasValue)
            filteredEvents = filteredEvents.Where(e => e.Timestamp >= since.Value);
        
        if (until.HasValue)
            filteredEvents = filteredEvents.Where(e => e.Timestamp <= until.Value);

        var events = filteredEvents.ToList();
        
        var eligibilityDistribution = new EligibilityDistribution(120, 30, 15); // Sample data
        var submissionsOverTime = CalculateSubmissionsOverTime(events);
        var appetiteShare = CalculateAppetiteShare();
        var rulesByProduct = CalculateRulesByProduct(events);

        var metrics = new AnalyticsMetrics(
            eligibilityDistribution,
            submissionsOverTime,
            appetiteShare,
            rulesByProduct
        );

        var response = new FetchAnalyticsResponse(DateTime.UtcNow, metrics);
        return await Task.FromResult(response);
    }

    private List<SubmissionOverTime> CalculateSubmissionsOverTime(List<StoredEvent> events)
    {
        return events
            .GroupBy(e => e.Timestamp.Date.ToString("yyyy-MM-dd"))
            .Select(g => new SubmissionOverTime(g.Key, g.Count()))
            .OrderBy(s => s.Date)
            .ToList();
    }

    private Dictionary<string, int> CalculateAppetiteShare()
    {
        // Sample data matching specification
        return new Dictionary<string, int>
        {
            {"Retail", 45},
            {"Restaurant", 20},
            {"Health", 10}
        };
    }

    private List<RuleByProduct> CalculateRulesByProduct(List<StoredEvent> events)
    {
        return events
            .Where(e => !string.IsNullOrEmpty(e.ProductId))
            .GroupBy(e => e.ProductId!)
            .Select(g => new RuleByProduct(g.Key, g.Count()))
            .OrderByDescending(r => r.RuleCount)
            .ToList();
    }
}