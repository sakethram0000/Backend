using Microsoft.AspNetCore.Mvc;
using MyWebApi.Models;
using MyWebApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CanvasController : ControllerBase
{
    private readonly ICanvasService _canvasService;

    public CanvasController(ICanvasService canvasService)
    {
        _canvasService = canvasService;
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _canvasService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Invalid credentials");
        }
    }

    /// <summary>
    /// Register new carrier or agent
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _canvasService.RegisterAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Fetch carrier profile by id
    /// </summary>
    [HttpGet("carrier/{id}")]
    [Authorize]
    public async Task<ActionResult<UserProfile>> GetCarrier(string id)
    {
        try
        {
            // Get current user from JWT claims
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            // Admin can access any carrier, others can only access their own data
            if (currentUserRole != "admin" && currentUserId != id)
            {
                return Forbid("You can only access your own profile");
            }
            
            var user = await _canvasService.GetCarrierAsync(id);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Carrier {id} not found");
        }
    }

    /// <summary>
    /// List all carriers (admin only) with pagination and optional role filter
    /// </summary>
    [HttpGet("carriers")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UsersResponse>> GetCarriers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? role = null)
    {
        var users = await _canvasService.GetCarriersAsync(page, pageSize, role);
        return Ok(users);
    }

    /// <summary>
    /// Create a new carrier
    /// </summary>
    [HttpPost("carriers")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserProfile>> CreateCarrier([FromBody] UserProfile carrier)
    {
        var result = await _canvasService.CreateCarrierAsync(carrier);
        return Ok(result);
    }

    /// <summary>
    /// Fetch product details (coverage, limits, eligibility)
    /// </summary>
    [HttpGet("product/{id}")]
    [Authorize]
    public async Task<ActionResult<ProductDetails>> GetProduct(string id)
    {
        try
        {
            var product = await _canvasService.GetProductAsync(id);
            return Ok(product);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Product {id} not found");
        }
    }

    /// <summary>
    /// List products with filtering by carrier or product type
    /// </summary>
    [HttpGet("products")]
    [Authorize]
    public async Task<ActionResult<ProductsResponse>> GetProducts(
        [FromQuery] string? carrier = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var products = await _canvasService.GetProductsAsync(carrier, page, pageSize);
        return Ok(products);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost("products")]
    [Authorize(Roles = "admin,carrier")]
    public async Task<ActionResult<ProductDetails>> CreateProduct([FromBody] ProductDetails product)
    {
        var result = await _canvasService.CreateProductAsync(product);
        return Ok(result);
    }

    /// <summary>
    /// Fetch rule details by id
    /// </summary>
    [HttpGet("rule/{id}")]
    [Authorize]
    public async Task<ActionResult<RuleDetails>> GetRule(string id)
    {
        try
        {
            var rule = await _canvasService.GetRuleAsync(id);
            return Ok(rule);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Rule {id} not found");
        }
    }

    /// <summary>
    /// List rules with pagination
    /// </summary>
    [HttpGet("rules")]
    [Authorize]
    public async Task<ActionResult<RulesResponse>> GetRules(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = null)
    {
        var rules = await _canvasService.GetRulesAsync(page, pageSize, sortBy);
        return Ok(rules);
    }

    /// <summary>
    /// Create a new rule
    /// </summary>
    [HttpPost("rules")]
    [Authorize(Roles = "admin,carrier")]
    public async Task<ActionResult<RuleDetails>> CreateRule([FromBody] RuleDetails rule)
    {
        var result = await _canvasService.CreateRuleAsync(rule);
        return Ok(result);
    }

    /// <summary>
    /// Update an existing rule
    /// </summary>
    [HttpPut("rule/{id}")]
    [Authorize(Roles = "admin,carrier")]
    public async Task<ActionResult<RuleDetails>> UpdateRule(string id, [FromBody] RuleDetails rule)
    {
        try
        {
            var result = await _canvasService.UpdateRuleAsync(id, rule);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Rule {id} not found");
        }
    }

    /// <summary>
    /// Delete a rule (admin only)
    /// </summary>
    [HttpDelete("rule/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> DeleteRule(string id)
    {
        try
        {
            await _canvasService.DeleteRuleAsync(id);
            return Ok(new { message = "Rule deleted successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Rule {id} not found");
        }
    }

    /// <summary>
    /// Bulk rule upload (CSV/Excel) with validation
    /// </summary>
    [HttpPost("rules/upload")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "admin,carrier")]
    public async Task<ActionResult<RuleUploadResponse>> UploadRules(IFormFile rulesFile, bool overwrite = false)
    {
        var result = await _canvasService.UploadRulesAsync(rulesFile, overwrite);
        return Ok(result);
    }

    /// <summary>
    /// Get carrier details by ID
    /// </summary>
    [HttpGet("carrier-details/{id}")]
    public async Task<ActionResult<CarrierDetails>> GetCarrierDetails(string id)
    {
        try
        {
            var carrier = await _canvasService.GetCarrierDetailsAsync(id);
            return Ok(carrier);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Carrier {id} not found");
        }
    }

    /// <summary>
    /// List carriers with pagination
    /// </summary>
    [HttpGet("carriers-list")]
    public async Task<ActionResult<CarriersResponse>> GetCarriersList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var carriers = await _canvasService.GetCarriersListAsync(page, pageSize);
        return Ok(carriers);
    }

    /// <summary>
    /// Create a new carrier
    /// </summary>
    [HttpPost("carrier-details")]
    public async Task<ActionResult<CarrierDetails>> CreateCarrierDetails([FromBody] CarrierDetails carrier)
    {
        var result = await _canvasService.CreateCarrierDetailsAsync(carrier);
        return Ok(result);
    }

    /// <summary>
    /// Update an existing carrier
    /// </summary>
    [HttpPut("carrier-details/{id}")]
    public async Task<ActionResult<CarrierDetails>> UpdateCarrierDetails(string id, [FromBody] CarrierDetails carrier)
    {
        try
        {
            var result = await _canvasService.UpdateCarrierDetailsAsync(id, carrier);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Carrier {id} not found");
        }
    }

    /// <summary>
    /// Delete a carrier (admin only)
    /// </summary>
    [HttpDelete("carrier-details/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> DeleteCarrierDetails(string id)
    {
        try
        {
            await _canvasService.DeleteCarrierDetailsAsync(id);
            return Ok(new { message = "Carrier deleted successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Carrier {id} not found");
        }
    }

    /// <summary>
    /// Provides latest aggregated analytics snapshot for dashboards and Power BI embedding
    /// </summary>
    [HttpGet("analytics")]
    [Authorize]
    public async Task<ActionResult<CanvasAnalyticsResponse>> GetAnalytics(
        [FromQuery] DateTime? since = null)
    {
        var analytics = await _canvasService.GetAnalyticsAsync(since);
        return Ok(analytics);
    }

    /// <summary>
    /// Quick user creation for testing (returns temporary password)
    /// </summary>
    [HttpPost("create-user")]
    public async Task<ActionResult<object>> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _canvasService.CreateUserAsync(request);
        return Ok(result);
    }
}