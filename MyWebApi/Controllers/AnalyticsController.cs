using Microsoft.AspNetCore.Mvc;
using MyWebApi.Models;
using MyWebApi.Services;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Accept raw analytics events (telemetry) from applications
    /// </summary>
    [HttpPost("add")]
    public async Task<ActionResult<AddEventResponse>> AddEvent([FromBody] AddEventRequest request)
    {
        var result = await _analyticsService.AddEventAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Return aggregated analytics (eligibility distribution, submissions over time, appetite share, rules by product)
    /// </summary>
    [HttpGet("fetch")]
    public async Task<ActionResult<FetchAnalyticsResponse>> FetchAnalytics(
        [FromQuery] DateTime? since = null,
        [FromQuery] DateTime? until = null)
    {
        var result = await _analyticsService.FetchAnalyticsAsync(since, until);
        return Ok(result);
    }
}