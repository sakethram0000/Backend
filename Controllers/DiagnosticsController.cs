using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Data;
using MyWebApi.Models;
using MyWebApi.Services;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;

    public DiagnosticsController(AppDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    [HttpGet("full-test")]
    public async Task<ActionResult> FullDatabaseTest()
    {
        var results = new List<object>();

        // Test 1: Database Connection
        try
        {
            await _context.Database.OpenConnectionAsync();
            await _context.Database.CloseConnectionAsync();
            results.Add(new { Test = "Database Connection", Status = "✅ SUCCESS" });
        }
        catch (Exception ex)
        {
            results.Add(new { Test = "Database Connection", Status = "❌ FAILED", Error = ex.Message });
            return Ok(new { Results = results });
        }

        // Test 2: Check Tables Exist
        try
        {
            var tableChecks = new Dictionary<string, bool>();
            
            // Check each table individually
            try { await _context.Users.Take(1).ToListAsync(); tableChecks["users"] = true; }
            catch { tableChecks["users"] = false; }
            
            try { await _context.Products.Take(1).ToListAsync(); tableChecks["products"] = true; }
            catch { tableChecks["products"] = false; }
            
            try { await _context.Rules.Take(1).ToListAsync(); tableChecks["rules"] = true; }
            catch { tableChecks["rules"] = false; }
            
            try { await _context.Carriers.Take(1).ToListAsync(); tableChecks["carriers"] = true; }
            catch { tableChecks["carriers"] = false; }

            results.Add(new { Test = "Tables Exist", Status = "✅ SUCCESS", Tables = tableChecks });
        }
        catch (Exception ex)
        {
            results.Add(new { Test = "Tables Exist", Status = "❌ FAILED", Error = ex.Message });
        }

        // Test 3: Count Records
        try
        {
            var counts = new
            {
                Users = await _context.Users.CountAsync(),
                Products = await _context.Products.CountAsync(),
                Rules = await _context.Rules.CountAsync(),
                Carriers = await _context.Carriers.CountAsync()
            };
            results.Add(new { Test = "Record Counts", Status = "✅ SUCCESS", Counts = counts });
        }
        catch (Exception ex)
        {
            results.Add(new { Test = "Record Counts", Status = "❌ FAILED", Error = ex.Message });
        }

        // Test 4: Test User Query (the problematic one)
        try
        {
            var testUser = await _context.Users
                .Where(u => u.Email == "admin@appetitechecker.com")
                .FirstOrDefaultAsync();
            
            results.Add(new { 
                Test = "User Query", 
                Status = "✅ SUCCESS", 
                Found = testUser != null,
                UserData = testUser != null ? new { testUser.Id, testUser.Email, testUser.IsActive } : null
            });
        }
        catch (Exception ex)
        {
            results.Add(new { Test = "User Query", Status = "❌ FAILED", Error = ex.Message });
        }

        // Test 5: Test Password Service
        try
        {
            var testPassword = "Admin123!";
            var hash = _passwordService.HashPassword(testPassword);
            var isValid = _passwordService.VerifyPassword(testPassword, hash);
            
            results.Add(new { 
                Test = "Password Service", 
                Status = isValid ? "✅ SUCCESS" : "❌ FAILED",
                HashGenerated = !string.IsNullOrEmpty(hash),
                VerificationWorks = isValid
            });
        }
        catch (Exception ex)
        {
            results.Add(new { Test = "Password Service", Status = "❌ FAILED", Error = ex.Message });
        }

        return Ok(new { 
            Timestamp = DateTime.UtcNow,
            Results = results,
            Summary = new {
                TotalTests = results.Count,
                Passed = results.Count(r => r.GetType().GetProperty("Status")?.GetValue(r)?.ToString()?.Contains("SUCCESS") == true),
                Failed = results.Count(r => r.GetType().GetProperty("Status")?.GetValue(r)?.ToString()?.Contains("FAILED") == true)
            }
        });
    }

    [HttpPost("create-test-user")]
    public async Task<ActionResult> CreateTestUser()
    {
        try
        {
            // Check if test user exists
            var existingUser = await _context.Users
                .Where(u => u.Email == "test@example.com")
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                return Ok(new { message = "Test user already exists", userId = existingUser.Id });
            }

            // Create test user
            var testUser = new DbUser
            {
                Id = "test-001",
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = _passwordService.HashPassword("Test123!"),
                Roles = "user",
                OrganizationId = "test-org",
                OrganizationName = "Test Organization",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AuthProvider = "local",
                FailedLoginAttempts = 0
            };

            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Test user created successfully", 
                userId = testUser.Id,
                email = testUser.Email,
                password = "Test123!"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("test-login")]
    public async Task<ActionResult> TestLogin([FromBody] TestLoginRequest request)
    {
        try
        {
            // Step 1: Find user
            var user = await _context.Users
                .Where(u => u.Email == request.Email)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return Ok(new { 
                    step = "Find User", 
                    status = "❌ FAILED", 
                    message = "User not found",
                    email = request.Email
                });
            }

            // Step 2: Check if active (in C# not SQL)
            if (!user.IsActive)
            {
                return Ok(new { 
                    step = "Check Active", 
                    status = "❌ FAILED", 
                    message = "User is inactive",
                    isActive = user.IsActive
                });
            }

            // Step 3: Verify password
            var passwordValid = _passwordService.VerifyPassword(request.Password, user.PasswordHash);
            if (!passwordValid)
            {
                return Ok(new { 
                    step = "Verify Password", 
                    status = "❌ FAILED", 
                    message = "Invalid password"
                });
            }

            return Ok(new { 
                step = "Complete", 
                status = "✅ SUCCESS", 
                message = "Login test successful",
                user = new { user.Id, user.Email, user.Name, user.Roles }
            });
        }
        catch (Exception ex)
        {
            return Ok(new { 
                step = "Exception", 
                status = "❌ FAILED", 
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}

public class TestLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}