using MyWebApi.Data;
using MyWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MyWebApi.Services;

public class SeedService
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;

    public SeedService(AppDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task SeedInitialDataAsync()
    {
        // Check if data already exists (avoid AnyAsync with potential boolean issues)
        var userCount = await _context.Users.CountAsync();
        if (userCount > 0)
            return;

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
        
        // Seed initial products
        var products = new[]
        {
            new DbProduct
            {
                Id = "prod-001",
                Name = "General Liability - SME",
                Carrier = "Acme Insurance",
                PerOccurrence = 1000000,
                Aggregate = 2000000,
                MinAnnualRevenue = 0,
                MaxAnnualRevenue = 5000000,
                NaicsAllowed = "445310,722511",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new DbProduct
            {
                Id = "prod-002",
                Name = "Property - Retail",
                Carrier = "Acme Insurance",
                PerOccurrence = 500000,
                Aggregate = 1000000,
                MinAnnualRevenue = 0,
                MaxAnnualRevenue = 2000000,
                NaicsAllowed = "445110,445120",
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new DbProduct
            {
                Id = "prod-003",
                Name = "Workers Comp - SME",
                Carrier = "Beta Mutual",
                PerOccurrence = 1000000,
                Aggregate = 1000000,
                MinAnnualRevenue = 0,
                MaxAnnualRevenue = 3000000,
                NaicsAllowed = "722511,561720",
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            }
        };
        
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }
}