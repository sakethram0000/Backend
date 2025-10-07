using Microsoft.AspNetCore.Mvc;
using MyWebApi.Models;
using MyWebApi.Services;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckerController : ControllerBase
{
    private readonly ICheckerService _checkerService;

    public CheckerController(ICheckerService checkerService)
    {
        _checkerService = checkerService;
    }

    /// <summary>
    /// Evaluate a submission using business description, NAICS code, and location
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<ActionResult<EvaluateResponse>> Evaluate([FromBody] EvaluateRequest request)
    {
        var result = await _checkerService.EvaluateAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Returns AI-based confidence score for classification accuracy
    /// </summary>
    [HttpGet("confidenceScore")]
    public async Task<ActionResult<ConfidenceScoreResponse>> GetConfidenceScore(
        [FromQuery] string naics,
        [FromQuery] string desc)
    {
        var result = await _checkerService.GetConfidenceScoreAsync(naics, desc);
        return Ok(result);
    }

    /// <summary>
    /// Validate rules for a product or line of business
    /// </summary>
    [HttpPost("eligibilityCheck")]
    public async Task<ActionResult<EligibilityCheckResponse>> CheckEligibility([FromBody] EligibilityCheckRequest request)
    {
        var result = await _checkerService.CheckEligibilityAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Suggest alternate classifications or products when appetite is restricted or declined
    /// </summary>
    [HttpGet("recommendations")]
    public async Task<ActionResult<RecommendationsResponse>> GetRecommendations([FromQuery] string submissionId)
    {
        var result = await _checkerService.GetRecommendationsAsync(submissionId);
        return Ok(result);
    }

    /// <summary>
    /// Generate a human-friendly AI summary of evaluation results
    /// </summary>
    [HttpPost("prepareSummary")]
    public async Task<ActionResult<PrepareSummaryResponse>> PrepareSummary([FromBody] PrepareSummaryRequest request)
    {
        var result = await _checkerService.PrepareSummaryAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve previously evaluated submission results
    /// </summary>
    [HttpGet("result/{id}")]
    public async Task<ActionResult<ResultResponse>> GetResult(string id)
    {
        try
        {
            var result = await _checkerService.GetResultAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Submission {id} not found");
        }
    }

    /// <summary>
    /// Send checker events to analytics pipeline
    /// </summary>
    [HttpPost("notifyAnalytics")]
    public async Task<ActionResult<NotifyAnalyticsResponse>> NotifyAnalytics([FromBody] NotifyAnalyticsRequest request)
    {
        var result = await _checkerService.NotifyAnalyticsAsync(request);
        return Ok(result);
    }
}