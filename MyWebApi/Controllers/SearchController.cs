using Microsoft.AspNetCore.Mvc;
using MyWebApi.Models;
using MyWebApi.Services;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    /// <summary>
    /// Retrieve all rules with pagination and optional free-text query
    /// </summary>
    [HttpGet("getRules")]
    public async Task<ActionResult<SearchRuleResponse>> GetRules(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? q = null)
    {
        var result = await _searchService.GetRulesAsync(page, pageSize, sortBy, q);
        return Ok(result);
    }

    /// <summary>
    /// Search rules by keyword across businessType, states, restrictions, title and description
    /// </summary>
    [HttpGet("getRulesByKeyword")]
    public async Task<ActionResult<SearchRuleResponse>> GetRulesByKeyword(
        [FromQuery] string keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _searchService.GetRulesByKeywordAsync(keyword, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve rules applicable to a specific NAICS code
    /// </summary>
    [HttpGet("getRulesByNaics/{naicsCode}")]
    public async Task<ActionResult<SearchRuleResponse>> GetRulesByNaics(
        string naicsCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _searchService.GetRulesByNaicsAsync(naicsCode, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve rules for a standardized business type
    /// </summary>
    [HttpGet("getRulesByBusinessType/{type}")]
    public async Task<ActionResult<SearchRuleResponse>> GetRulesByBusinessType(
        string type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _searchService.GetRulesByBusinessTypeAsync(type, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Advanced filtering by carrier, product, geography, NAICS and business types
    /// </summary>
    [HttpPost("getRulesByCustomFilter")]
    public async Task<ActionResult<SearchRuleResponse>> GetRulesByCustomFilter([FromBody] CustomFilterRequest request)
    {
        var result = await _searchService.GetRulesByCustomFilterAsync(request);
        return Ok(result);
    }
}