using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Data;
using MyWebApi.Models;
using MyWebApi.Services;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly IIdGenerationService _idGenerationService;

    public AuthController(
        AppDbContext context,
        IPasswordService passwordService,
        IJwtService jwtService,
        IIdGenerationService idGenerationService)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _idGenerationService = idGenerationService;
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }
/*
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
            
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Check if account is locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Account is temporarily locked" });
            }

            // Verify password
            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                // Increment failed attempts
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await _context.SaveChangesAsync();
                
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Reset failed attempts on successful login
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
*/
            // Generate JWT token
            var token = _jwtService.GenerateToken(user.Id, user.Email, user.Roles ?? "User");

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    roles = user.Roles,
                    organizationId = user.OrganizationId,
                    organizationName = user.OrganizationName
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Login failed", error = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "User with this email already exists" });
            }

            // Create new user
            var user = new DbUser
            {
                Id = _idGenerationService.GenerateId("user"),
                Name = request.Name ?? request.Email,
                Email = request.Email,
                PasswordHash = _passwordService.HashPassword(request.Password),
                Roles = "User",
                OrganizationId = request.OrganizationId,
                OrganizationName = request.OrganizationName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _jwtService.GenerateToken(user.Id, user.Email, user.Roles);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    roles = user.Roles,
                    organizationId = user.OrganizationId,
                    organizationName = user.OrganizationName
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Registration failed", error = ex.Message });
        }
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string? Name { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
}