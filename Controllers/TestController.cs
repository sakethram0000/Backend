using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Data;
using MyWebApi.Models;
using MyWebApi.Services;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;

    public TestController(AppDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    [HttpGet("db-status")]
    public async Task<ActionResult> GetDatabaseStatus()
    {
        try
        {
            // Test database connection
            await _context.Database.OpenConnectionAsync();
            await _context.Database.CloseConnectionAsync();

            // Check if tables exist
            var tablesExist = new
            {
                UsersTable = await _context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'users'").FirstOrDefaultAsync() > 0,
                ProductsTable = await _context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'products'").FirstOrDefaultAsync() > 0,
                RulesTable = await _context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'rules'").FirstOrDefaultAsync() > 0,
                CarriersTable = await _context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'carriers'").FirstOrDefaultAsync() > 0
            };

            var userCount = 0;
            try
            {
                userCount = await _context.Users.CountAsync();
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    DatabaseConnected = true,
                    TablesExist = tablesExist,
                    UserCount = "Error: " + ex.Message,
                    NeedsMigration = true
                });
            }

            return Ok(new
            {
                DatabaseConnected = true,
                TablesExist = tablesExist,
                UserCount = userCount,
                NeedsMigration = false
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                DatabaseConnected = false,
                Error = ex.Message,
                NeedsMigration = true
            });
        }
    }

    [HttpPost("create-tables")]
    public async Task<ActionResult> CreateTables()
    {
        try
        {
            // Ensure database is created and apply migrations
            await _context.Database.EnsureCreatedAsync();
            
            return Ok(new { message = "Tables created successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("seed-users")]
    public async Task<ActionResult> SeedUsers()
    {
        try
        {
            // Check if users already exist
            var userCount = await _context.Users.CountAsync();
            if (userCount > 0)
            {
                return Ok(new { message = "Users already exist", count = userCount });
            }

            var users = new[]
            {
                new DbUser
                {
                    Id = "usr-001",
                    Name = "System Admin",
                    Email = "admin@appetitechecker.com",
                    PasswordHash = _passwordService.HashPassword("Admin123!"),
                    Roles = "admin",
                    OrganizationId = "org-001",
                    OrganizationName = "System Organization",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AuthProvider = "local",
                    FailedLoginAttempts = 0
                },
                new DbUser
                {
                    Id = "usr-002",
                    Name = "John Carrier",
                    Email = "carrier@example.com",
                    PasswordHash = _passwordService.HashPassword("Admin123!"),
                    Roles = "carrier",
                    OrganizationId = "org-002",
                    OrganizationName = "ABC Insurance",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AuthProvider = "local",
                    FailedLoginAttempts = 0
                },
                new DbUser
                {
                    Id = "usr-003",
                    Name = "Jane Agent",
                    Email = "agent@example.com",
                    PasswordHash = _passwordService.HashPassword("Admin123!"),
                    Roles = "agent",
                    OrganizationId = "org-003",
                    OrganizationName = "XYZ Brokerage",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AuthProvider = "local",
                    FailedLoginAttempts = 0
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Test users created successfully", count = users.Length });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}