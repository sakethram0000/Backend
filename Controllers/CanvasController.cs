using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyWebApi.Models;
using MyWebApi.Services;
using System.Security.Claims;

namespace MyWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication by default
    public class CanvasController : ControllerBase
    {
        private readonly ICanvasService _canvasService;
        private readonly IIdGenerationService _idGenerationService;

        public CanvasController(ICanvasService canvasService, IIdGenerationService idGenerationService)
        {
            _canvasService = canvasService;
            _idGenerationService = idGenerationService;
        }

        #region Helper Methods
        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        #endregion

        #region Auth Endpoints

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] MyWebApi.Models.LoginRequest request)
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

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] MyWebApi.Models.RegisterRequest request)
        {
            var result = await _canvasService.RegisterAsync(request);
            return Ok(result);
        }

        #endregion

        #region Carrier Endpoints

        [HttpGet("carrier/{id}")]
        public async Task<ActionResult<UserProfile>> GetCarrier(string id)
        {
            var currentUserId = GetUserId();
            var currentUserRole = GetUserRole();

            if (currentUserRole != "admin" && currentUserId != id)
                return Forbid("You can only access your own profile");

            try
            {
                var user = await _canvasService.GetCarrierAsync(id);
                return Ok(user);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Carrier {id} not found");
            }
        }

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

        [HttpPost("carriers")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<UserProfile>> CreateCarrier([FromBody] UserProfile carrier)
        {
            // Use IdGenerationService for generating CarrierId
            carrier.Id = await _idGenerationService.GenerateCarrierIdAsync();

            var result = await _canvasService.CreateCarrierAsync(carrier);
            return Ok(result);
        }

        [HttpGet("carrier-details/{id}")]
        public async Task<ActionResult<CarrierDetails>> GetCarrierDetails(string id)
        {
            var currentUserId = GetUserId();
            var currentUserRole = GetUserRole();

            if (currentUserRole != "admin" && currentUserId != id)
                return Forbid("You can only access your own profile");

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

        [HttpGet("carriers-list")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<CarriersResponse>> GetCarriersList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            var carriers = await _canvasService.GetCarriersListAsync(page, pageSize);
            return Ok(carriers);
        }

        [HttpPost("carrier-details")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<CarrierDetails>> CreateCarrierDetails([FromBody] CarrierDetails carrier)
        {
            carrier.Id = await _idGenerationService.GenerateCarrierIdAsync();
            var result = await _canvasService.CreateCarrierDetailsAsync(carrier);
            return Ok(result);
        }

        [HttpPut("carrier-details/{id}")]
        [Authorize(Roles = "admin")]
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

        #endregion

        #region Product Endpoints

        [HttpGet("product/{id}")]
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

        [HttpGet("products")]
        public async Task<ActionResult<ProductsResponse>> GetProducts(
            [FromQuery] string? carrier = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var products = await _canvasService.GetProductsAsync(carrier, page, pageSize);
            return Ok(products);
        }

        [HttpPost("products")]
        [Authorize(Roles = "admin,carrier")]
        public async Task<ActionResult<ProductDetails>> CreateProduct([FromBody] ProductDetails product)
        {
            product.Id = await _idGenerationService.GenerateProductIdAsync();
            var result = await _canvasService.CreateProductAsync(product);
            return Ok(result);
        }

        #endregion

        #region Rule Endpoints

        [HttpGet("rule/{id}")]
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

        [HttpGet("rules")]
        public async Task<ActionResult<RulesResponse>> GetRules(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? sortBy = null)
        {
            var rules = await _canvasService.GetRulesAsync(page, pageSize, sortBy);
            return Ok(rules);
        }

        [HttpPost("rules")]
        [Authorize(Roles = "admin,carrier")]
        public async Task<ActionResult<RuleDetails>> CreateRule([FromBody] RuleDetails rule)
        {
            rule.Id = await _idGenerationService.GenerateRuleIdAsync();
            var result = await _canvasService.CreateRuleAsync(rule);
            return Ok(result);
        }

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

        [HttpPost("rules/upload")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "admin,carrier")]
        public async Task<ActionResult<RuleUploadResponse>> UploadRules(IFormFile rulesFile, bool overwrite = false)
        {
            if (rulesFile == null || rulesFile.Length == 0)
                return BadRequest("Rules file is required");

            var result = await _canvasService.UploadRulesAsync(rulesFile, overwrite);
            return Ok(result);
        }

        #endregion

        #region Analytics & Utility

        [HttpGet("analytics")]
        public async Task<ActionResult<CanvasAnalyticsResponse>> GetAnalytics([FromQuery] DateTime? since = null)
        {
            var analytics = await _canvasService.GetAnalyticsAsync(since);
            return Ok(analytics);
        }

        [HttpPost("create-user")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<object>> CreateUser([FromBody] CreateUserRequest request)
        {
            // Generate new user ID
            request.Id = await _idGenerationService.GenerateUserIdAsync();
            var result = await _canvasService.CreateUserAsync(request);
            return Ok(result);
        }

        #endregion
    }
}
