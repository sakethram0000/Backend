using MyWebApi.Models;
using MyWebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace MyWebApi.Services;

public interface ICanvasService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<UserProfile> GetCarrierAsync(string id);
    Task<UsersResponse> GetCarriersAsync(int page, int pageSize, string? role);
    Task<UserProfile> CreateCarrierAsync(UserProfile carrier);
    Task<ProductDetails> GetProductAsync(string id);
    Task<ProductsResponse> GetProductsAsync(string? carrier, int page, int pageSize);
    Task<ProductDetails> CreateProductAsync(ProductDetails product);
    Task<RuleDetails> GetRuleAsync(string id);
    Task<RulesResponse> GetRulesAsync(int page, int pageSize, string? sortBy);
    Task<RuleDetails> CreateRuleAsync(RuleDetails rule);
    Task<RuleDetails> UpdateRuleAsync(string id, RuleDetails rule);
    Task DeleteRuleAsync(string id);
    Task<RuleUploadResponse> UploadRulesAsync(IFormFile file, bool overwrite);
    Task<CarrierDetails> GetCarrierDetailsAsync(string id);
    Task<CarriersResponse> GetCarriersListAsync(int page, int pageSize);
    Task<CarrierDetails> CreateCarrierDetailsAsync(CarrierDetails carrier);
    Task<CarrierDetails> UpdateCarrierDetailsAsync(string id, CarrierDetails carrier);
    Task DeleteCarrierDetailsAsync(string id);
    Task<CanvasAnalyticsResponse> GetAnalyticsAsync(DateTime? since);
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request, string? userId = null);
}

public class CanvasService : ICanvasService
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly IIdGenerationService _idGenerationService;

    public CanvasService(AppDbContext context, IPasswordService passwordService, IJwtService jwtService, IIdGenerationService idGenerationService)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _idGenerationService = idGenerationService;
    }

    private string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }



    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Username && u.IsActive);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid credentials");

        // Check account lockout
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            throw new UnauthorizedAccessException("Account is locked. Try again later.");

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
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Reset failed attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        var roles = user.Roles?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var userInfo = new UserInfo(user.Id, user.Name, roles, user.IsActive, user.LastLoginAt, user.AuthProvider);
        return new LoginResponse(token, "Bearer", 3600, userInfo);
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Admin.Email);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

        var userId = await _idGenerationService.GenerateUserIdAsync();
        var orgId = await _idGenerationService.GenerateOrganizationIdAsync();
        
        // Generate temporary password (should be sent via email in production)
        var tempPassword = GenerateTemporaryPassword();
        var hashedPassword = _passwordService.HashPassword(tempPassword);
        
        var newUser = new DbUser
        {
            Id = userId,
            Name = request.Admin.Name,
            Email = request.Admin.Email,
            PasswordHash = hashedPassword,
            Roles = "admin",
            OrganizationId = orgId,
            OrganizationName = request.OrganizationName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            AuthProvider = "local"
        };
        
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        
        return new RegisterResponse(userId, "active", $"Registration successful. Temporary password: {tempPassword}");
    }

    public async Task<UserProfile> GetCarrierAsync(string id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            throw new KeyNotFoundException($"Carrier {id} not found");
        
        var roles = user.Roles?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var profile = new UserProfile(user.Id, user.Name, user.Email, roles, 
            new OrganizationInfo(user.OrganizationId ?? "", user.OrganizationName ?? ""), 
            user.CreatedAt, user.IsActive, user.LastLoginAt, user.AuthProvider);
        return profile;
    }

    public async Task<UsersResponse> GetCarriersAsync(int page, int pageSize, string? role)
    {
        var query = _context.Users.AsQueryable();
        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Roles != null && u.Roles.Contains(role));

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserSummary(u.Id, u.Name, u.Email, 
                u.Roles != null ? u.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>(), 
                u.IsActive))
            .ToArrayAsync();

        var pagination = new PaginationInfo(page, pageSize, totalPages, totalItems);
        return new UsersResponse(users, pagination);
    }

    public async Task<UserProfile> CreateCarrierAsync(UserProfile carrier)
    {
        var userId = await _idGenerationService.GenerateUserIdAsync();
        var carrierId = await _idGenerationService.GenerateCarrierIdAsync();
        var orgId = await _idGenerationService.GenerateOrganizationIdAsync();
        
        // Generate temporary password
        var tempPassword = GenerateTemporaryPassword();
        var hashedPassword = _passwordService.HashPassword(tempPassword);
        
        // Create User record
        var dbUser = new DbUser
        {
            Id = userId,
            Name = carrier.Name,
            Email = carrier.Email,
            PasswordHash = hashedPassword,
            Roles = string.Join(",", carrier.Roles),
            OrganizationId = orgId,
            OrganizationName = carrier.Organization.Name,
            CreatedAt = DateTime.UtcNow,
            IsActive = carrier.IsActive,
            AuthProvider = carrier.AuthProvider ?? "local"
        };
        
        // Create Carrier record (if user has carrier role)
        DbCarrier? dbCarrier = null;
        if (carrier.Roles.Contains("carrier"))
        {
            dbCarrier = new DbCarrier
            {
                CarrierId = carrierId,
                LegalName = carrier.Organization.Name,
                DisplayName = carrier.Organization.Name,
                PrimaryContactName = carrier.Name,
                PrimaryContactEmail = carrier.Email,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            };
            _context.Carriers.Add(dbCarrier);
        }
        
        _context.Users.Add(dbUser);
        await _context.SaveChangesAsync();
        
        var createdCarrier = new UserProfile(dbUser.Id, dbUser.Name, dbUser.Email, carrier.Roles, 
            new OrganizationInfo(dbUser.OrganizationId ?? "", dbUser.OrganizationName ?? ""), 
            dbUser.CreatedAt, dbUser.IsActive, dbUser.LastLoginAt, dbUser.AuthProvider);
        return createdCarrier;
    }

    public async Task<ProductDetails> GetProductAsync(string id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            throw new KeyNotFoundException($"Product {id} not found");
        
        return new ProductDetails(product.Id, product.Name, "", "", product.Carrier, product.PerOccurrence, product.Aggregate, product.MinAnnualRevenue, product.MaxAnnualRevenue, product.NaicsAllowed, product.CreatedAt);
    }

    public async Task<ProductsResponse> GetProductsAsync(string? carrier, int page, int pageSize)
    {
        var query = _context.Products.AsQueryable();
        if (!string.IsNullOrEmpty(carrier))
            query = query.Where(p => p.Carrier == carrier);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        
        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductSummary(p.Id, p.Name, p.Carrier))
            .ToArrayAsync();

        var pagination = new PaginationInfo(page, pageSize, totalPages, totalItems);
        return new ProductsResponse(products, pagination);
    }

    public async Task<ProductDetails> CreateProductAsync(ProductDetails product)
    {
        var newId = await _idGenerationService.GenerateProductIdAsync();
        var dbProduct = new DbProduct
        {
            Id = newId,
            Name = product.Name,
            Carrier = product.Carrier,
            PerOccurrence = product.PerOccurrence,
            Aggregate = product.Aggregate,
            MinAnnualRevenue = product.MinAnnualRevenue,
            MaxAnnualRevenue = product.MaxAnnualRevenue,
            NaicsAllowed = product.NaicsAllowed,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Products.Add(dbProduct);
        await _context.SaveChangesAsync();
        
        return new ProductDetails(dbProduct.Id, dbProduct.Name, product.Description, product.ProductType, dbProduct.Carrier, dbProduct.PerOccurrence, dbProduct.Aggregate, dbProduct.MinAnnualRevenue, dbProduct.MaxAnnualRevenue, dbProduct.NaicsAllowed, dbProduct.CreatedAt);
    }

    public async Task<RuleDetails> GetRuleAsync(string id)
    {
        var rule = await _context.Rules.FirstOrDefaultAsync(r => r.RuleId == id);
        if (rule == null)
            throw new KeyNotFoundException($"Rule {id} not found");
        
        var details = new RuleDetails(rule.RuleId, rule.Title, rule.Description, rule.BusinessType, 
            rule.NaicsCodes?.Split(';'), rule.States?.Split(';'), rule.Carrier, rule.Product, 
            rule.Restrictions?.Split(';'), rule.Priority, rule.Outcome, rule.RuleVersion, rule.Status,
            rule.EffectiveFrom, rule.EffectiveTo, rule.MinRevenue, rule.MaxRevenue, 
            rule.MinYearsInBusiness, rule.MaxYearsInBusiness, rule.PriorClaimsAllowed,
            rule.Conditions?.Split(';'), rule.ContactEmail, rule.CreatedBy, rule.CreatedAt, rule.UpdatedAt, rule.AdditionalJson);
        return details;
    }

    public async Task<RulesResponse> GetRulesAsync(int page, int pageSize, string? sortBy)
    {
        var query = _context.Rules.AsQueryable();
        
        if (!string.IsNullOrEmpty(sortBy))
        {
            if (sortBy == "priority:desc")
                query = query.OrderByDescending(r => r.Priority == "high" ? 3 : r.Priority == "medium" ? 2 : 1);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        
        var rules = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RuleSummary(r.RuleId, r.Title, r.Priority, r.Status))
            .ToArrayAsync();

        var pagination = new PaginationInfo(page, pageSize, totalPages, totalItems);
        return new RulesResponse(rules, pagination);
    }

    public async Task<RuleDetails> CreateRuleAsync(RuleDetails rule)
    {
        var newId = await _idGenerationService.GenerateRuleIdAsync();
        var dbRule = new DbRule
        {
            RuleId = newId,
            Title = rule.Title,
            Description = rule.Description,
            BusinessType = rule.BusinessType,
            NaicsCodes = rule.NaicsCodes != null ? string.Join(";", rule.NaicsCodes) : null,
            States = rule.States != null ? string.Join(";", rule.States) : null,
            Carrier = rule.Carrier,
            Product = rule.Product,
            Restrictions = rule.Restrictions != null ? string.Join(";", rule.Restrictions) : null,
            Priority = rule.Priority,
            Outcome = rule.Outcome,
            RuleVersion = rule.RuleVersion,
            Status = rule.Status ?? "Draft",
            EffectiveFrom = rule.EffectiveFrom,
            EffectiveTo = rule.EffectiveTo,
            MinRevenue = rule.MinRevenue,
            MaxRevenue = rule.MaxRevenue,
            MinYearsInBusiness = rule.MinYearsInBusiness,
            MaxYearsInBusiness = rule.MaxYearsInBusiness,
            PriorClaimsAllowed = rule.PriorClaimsAllowed,
            Conditions = rule.Conditions != null ? string.Join(";", rule.Conditions) : null,
            ContactEmail = rule.ContactEmail,
            CreatedBy = rule.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            AdditionalJson = rule.AdditionalJson
        };
        
        _context.Rules.Add(dbRule);
        await _context.SaveChangesAsync();
        
        return new RuleDetails(dbRule.RuleId, dbRule.Title, dbRule.Description, dbRule.BusinessType,
            dbRule.NaicsCodes?.Split(';'), dbRule.States?.Split(';'), dbRule.Carrier, dbRule.Product,
            dbRule.Restrictions?.Split(';'), dbRule.Priority, dbRule.Outcome, dbRule.RuleVersion, dbRule.Status,
            dbRule.EffectiveFrom, dbRule.EffectiveTo, dbRule.MinRevenue, dbRule.MaxRevenue,
            dbRule.MinYearsInBusiness, dbRule.MaxYearsInBusiness, dbRule.PriorClaimsAllowed,
            dbRule.Conditions?.Split(';'), dbRule.ContactEmail, dbRule.CreatedBy, dbRule.CreatedAt, dbRule.UpdatedAt, dbRule.AdditionalJson);
    }

    public async Task<RuleUploadResponse> UploadRulesAsync(IFormFile file, bool overwrite)
    {
        var uploadId = $"upl-{DateTime.UtcNow.Ticks}";
        var errors = new UploadError[]
        {
            new(102, "Missing naicsCodes"),
            new(205, "Invalid state code 'XX'")
        };
        
        var reportUrl = $"https://canvas.example.com/v1/rules/upload/{uploadId}/report";
        return await Task.FromResult(new RuleUploadResponse(uploadId, "processed", 45, 5, 2, errors, reportUrl));
    }

    public async Task<CanvasAnalyticsResponse> GetAnalyticsAsync(DateTime? since)
    {
        var sinceDate = since ?? DateTime.UtcNow.AddDays(-30);
        
        // Real database metrics
        var totalUsers = await _context.Users.CountAsync();
        var totalCarriers = await _context.Carriers.CountAsync();
        var totalRules = await _context.Rules.CountAsync();
        var totalProducts = await _context.Products.CountAsync();
        
        // Rules by priority (real data)
        var rulesByPriority = await _context.Rules
            .GroupBy(r => r.Priority ?? "medium")
            .ToDictionaryAsync(g => g.Key, g => g.Count());
        
        // Ensure we have data even if no rules exist
        if (!rulesByPriority.Any())
        {
            rulesByPriority = new Dictionary<string, int> { { "No Rules", 0 } };
        }
        
        // Users by role (real data)
        var usersByRole = await _context.Users
            .GroupBy(u => u.Roles ?? "user")
            .ToDictionaryAsync(g => g.Key, g => g.Count());
        
        // Recent activity (real data)
        var recentRules = await _context.Rules
            .Where(r => r.CreatedAt >= sinceDate)
            .CountAsync();
        
        // Real growth data (last 7 days)
        var growthData = new List<RealGrowthData>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddDays(-i);
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);
            
            var dailyUsers = await _context.Users
                .Where(u => u.CreatedAt >= dayStart && u.CreatedAt < dayEnd)
                .CountAsync();
            
            var dailyRules = await _context.Rules
                .Where(r => r.CreatedAt >= dayStart && r.CreatedAt < dayEnd)
                .CountAsync();
            
            var dailyCarriers = await _context.Carriers
                .Where(c => c.CreatedAt >= dayStart && c.CreatedAt < dayEnd)
                .CountAsync();
            
            growthData.Add(new RealGrowthData(
                date.ToString("yyyy-MM-dd"),
                dailyUsers,
                dailyRules,
                dailyCarriers
            ));
        }
        
        var response = new CanvasAnalyticsResponse(
            DateTime.UtcNow,
            new CanvasMetrics(
                TotalRules: totalRules,
                RulesByPriority: rulesByPriority,
                ProductsByCarrier: new Dictionary<string, int> { { "Products", totalProducts } },
                RecentUploads: recentRules,
                TotalUsers: totalUsers,
                TotalCarriers: totalCarriers,
                TotalProducts: totalProducts,
                UsersByRole: usersByRole,
                GrowthData: growthData.ToArray()
            ),
            "https://app.powerbi.com/reportEmbed?reportId=abcd-1234"
        );
        
        return response;
    }

    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request, string? userId = null)
    {
        // Check if user exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
            throw new InvalidOperationException("User already exists");

        var finalUserId = userId ?? await _idGenerationService.GenerateUserIdAsync();
        var orgId = await _idGenerationService.GenerateOrganizationIdAsync();
        var tempPassword = GenerateTemporaryPassword();
        
        var user = new DbUser
        {
            Id = finalUserId,
            Name = request.Name,
            Email = request.Email,
            PasswordHash = _passwordService.HashPassword(tempPassword),
            Roles = request.Role,
            OrganizationId = orgId,
            OrganizationName = request.OrganizationName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            AuthProvider = "local",
            FailedLoginAttempts = 0
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return new CreateUserResponse(finalUserId, request.Email, tempPassword, "User created successfully");
    }

    public async Task<CarrierDetails> GetCarrierDetailsAsync(string id)
    {
        var carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.CarrierId == id);
        if (carrier == null)
            throw new KeyNotFoundException($"Carrier {id} not found");
        
        return new CarrierDetails(carrier.CarrierId, carrier.LegalName, carrier.DisplayName, carrier.Country,
            carrier.HeadquartersAddress, carrier.PrimaryContactName, carrier.PrimaryContactEmail, carrier.PrimaryContactPhone,
            carrier.TechnicalContactName, carrier.TechnicalContactEmail, carrier.AuthMethod, carrier.SsoMetadataUrl,
            carrier.ApiClientId, carrier.ApiSecretKeyRef, carrier.DataResidency, carrier.ProductsOffered?.Split(';'),
            carrier.RuleUploadAllowed, carrier.RuleUploadMethod, carrier.RuleApprovalRequired, carrier.DefaultRuleVersioning,
            carrier.UseNaicsEnrichment, carrier.PreferredNaicsSource, carrier.PasWebhookUrl, carrier.WebhookAuthType,
            carrier.WebhookSecretRef, carrier.ContractRef, carrier.BillingContactEmail, carrier.RetentionPolicyDays,
            carrier.CreatedBy, carrier.CreatedAt, carrier.UpdatedAt, carrier.AdditionalJson);
    }

    public async Task<CarriersResponse> GetCarriersListAsync(int page, int pageSize)
    {
        var query = _context.Carriers.AsQueryable();
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        
        var carriers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CarrierSummary(c.CarrierId, c.LegalName, c.DisplayName, c.Country, c.PrimaryContactEmail))
            .ToArrayAsync();

        var pagination = new PaginationInfo(page, pageSize, totalPages, totalItems);
        return new CarriersResponse(carriers, pagination);
    }

    public async Task<CarrierDetails> CreateCarrierDetailsAsync(CarrierDetails carrier)
    {
        var newId = $"car-{DateTime.UtcNow.Ticks}";
        var dbCarrier = new DbCarrier
        {
            CarrierId = newId,
            LegalName = carrier.LegalName,
            DisplayName = carrier.DisplayName,
            Country = carrier.Country,
            HeadquartersAddress = carrier.HeadquartersAddress,
            PrimaryContactName = carrier.PrimaryContactName,
            PrimaryContactEmail = carrier.PrimaryContactEmail,
            PrimaryContactPhone = carrier.PrimaryContactPhone,
            TechnicalContactName = carrier.TechnicalContactName,
            TechnicalContactEmail = carrier.TechnicalContactEmail,
            AuthMethod = carrier.AuthMethod,
            SsoMetadataUrl = carrier.SsoMetadataUrl,
            ApiClientId = carrier.ApiClientId,
            ApiSecretKeyRef = carrier.ApiSecretKeyRef,
            DataResidency = carrier.DataResidency,
            ProductsOffered = carrier.ProductsOffered != null ? string.Join(";", carrier.ProductsOffered) : null,
            RuleUploadAllowed = carrier.RuleUploadAllowed,
            RuleUploadMethod = carrier.RuleUploadMethod,
            RuleApprovalRequired = carrier.RuleApprovalRequired,
            DefaultRuleVersioning = carrier.DefaultRuleVersioning,
            UseNaicsEnrichment = carrier.UseNaicsEnrichment,
            PreferredNaicsSource = carrier.PreferredNaicsSource,
            PasWebhookUrl = carrier.PasWebhookUrl,
            WebhookAuthType = carrier.WebhookAuthType,
            WebhookSecretRef = carrier.WebhookSecretRef,
            ContractRef = carrier.ContractRef,
            BillingContactEmail = carrier.BillingContactEmail,
            RetentionPolicyDays = carrier.RetentionPolicyDays,
            CreatedBy = carrier.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            AdditionalJson = carrier.AdditionalJson
        };
        
        _context.Carriers.Add(dbCarrier);
        await _context.SaveChangesAsync();
        
        return new CarrierDetails(dbCarrier.CarrierId, dbCarrier.LegalName, dbCarrier.DisplayName, dbCarrier.Country,
            dbCarrier.HeadquartersAddress, dbCarrier.PrimaryContactName, dbCarrier.PrimaryContactEmail, dbCarrier.PrimaryContactPhone,
            dbCarrier.TechnicalContactName, dbCarrier.TechnicalContactEmail, dbCarrier.AuthMethod, dbCarrier.SsoMetadataUrl,
            dbCarrier.ApiClientId, dbCarrier.ApiSecretKeyRef, dbCarrier.DataResidency, dbCarrier.ProductsOffered?.Split(';'),
            dbCarrier.RuleUploadAllowed, dbCarrier.RuleUploadMethod, dbCarrier.RuleApprovalRequired, dbCarrier.DefaultRuleVersioning,
            dbCarrier.UseNaicsEnrichment, dbCarrier.PreferredNaicsSource, dbCarrier.PasWebhookUrl, dbCarrier.WebhookAuthType,
            dbCarrier.WebhookSecretRef, dbCarrier.ContractRef, dbCarrier.BillingContactEmail, dbCarrier.RetentionPolicyDays,
            dbCarrier.CreatedBy, dbCarrier.CreatedAt, dbCarrier.UpdatedAt, dbCarrier.AdditionalJson);
    }

    public async Task<CarrierDetails> UpdateCarrierDetailsAsync(string id, CarrierDetails carrier)
    {
        var existingCarrier = await _context.Carriers.FirstOrDefaultAsync(c => c.CarrierId == id);
        if (existingCarrier == null)
            throw new KeyNotFoundException($"Carrier {id} not found");
        
        existingCarrier.LegalName = carrier.LegalName;
        existingCarrier.DisplayName = carrier.DisplayName;
        existingCarrier.Country = carrier.Country;
        existingCarrier.HeadquartersAddress = carrier.HeadquartersAddress;
        existingCarrier.PrimaryContactName = carrier.PrimaryContactName;
        existingCarrier.PrimaryContactEmail = carrier.PrimaryContactEmail;
        existingCarrier.PrimaryContactPhone = carrier.PrimaryContactPhone;
        existingCarrier.TechnicalContactName = carrier.TechnicalContactName;
        existingCarrier.TechnicalContactEmail = carrier.TechnicalContactEmail;
        existingCarrier.AuthMethod = carrier.AuthMethod;
        existingCarrier.SsoMetadataUrl = carrier.SsoMetadataUrl;
        existingCarrier.ApiClientId = carrier.ApiClientId;
        existingCarrier.ApiSecretKeyRef = carrier.ApiSecretKeyRef;
        existingCarrier.DataResidency = carrier.DataResidency;
        existingCarrier.ProductsOffered = carrier.ProductsOffered != null ? string.Join(";", carrier.ProductsOffered) : null;
        existingCarrier.RuleUploadAllowed = carrier.RuleUploadAllowed;
        existingCarrier.RuleUploadMethod = carrier.RuleUploadMethod;
        existingCarrier.RuleApprovalRequired = carrier.RuleApprovalRequired;
        existingCarrier.DefaultRuleVersioning = carrier.DefaultRuleVersioning;
        existingCarrier.UseNaicsEnrichment = carrier.UseNaicsEnrichment;
        existingCarrier.PreferredNaicsSource = carrier.PreferredNaicsSource;
        existingCarrier.PasWebhookUrl = carrier.PasWebhookUrl;
        existingCarrier.WebhookAuthType = carrier.WebhookAuthType;
        existingCarrier.WebhookSecretRef = carrier.WebhookSecretRef;
        existingCarrier.ContractRef = carrier.ContractRef;
        existingCarrier.BillingContactEmail = carrier.BillingContactEmail;
        existingCarrier.RetentionPolicyDays = carrier.RetentionPolicyDays;
        existingCarrier.UpdatedAt = DateTime.UtcNow;
        existingCarrier.AdditionalJson = carrier.AdditionalJson;
        
        await _context.SaveChangesAsync();
        
        return new CarrierDetails(existingCarrier.CarrierId, existingCarrier.LegalName, existingCarrier.DisplayName, existingCarrier.Country,
            existingCarrier.HeadquartersAddress, existingCarrier.PrimaryContactName, existingCarrier.PrimaryContactEmail, existingCarrier.PrimaryContactPhone,
            existingCarrier.TechnicalContactName, existingCarrier.TechnicalContactEmail, existingCarrier.AuthMethod, existingCarrier.SsoMetadataUrl,
            existingCarrier.ApiClientId, existingCarrier.ApiSecretKeyRef, existingCarrier.DataResidency, existingCarrier.ProductsOffered?.Split(';'),
            existingCarrier.RuleUploadAllowed, existingCarrier.RuleUploadMethod, existingCarrier.RuleApprovalRequired, existingCarrier.DefaultRuleVersioning,
            existingCarrier.UseNaicsEnrichment, existingCarrier.PreferredNaicsSource, existingCarrier.PasWebhookUrl, existingCarrier.WebhookAuthType,
            existingCarrier.WebhookSecretRef, existingCarrier.ContractRef, existingCarrier.BillingContactEmail, existingCarrier.RetentionPolicyDays,
            existingCarrier.CreatedBy, existingCarrier.CreatedAt, existingCarrier.UpdatedAt, existingCarrier.AdditionalJson);
    }

    public async Task<RuleDetails> UpdateRuleAsync(string id, RuleDetails rule)
    {
        var existingRule = await _context.Rules.FirstOrDefaultAsync(r => r.RuleId == id);
        if (existingRule == null)
            throw new KeyNotFoundException($"Rule {id} not found");
        
        existingRule.Title = rule.Title;
        existingRule.Description = rule.Description;
        existingRule.BusinessType = rule.BusinessType;
        existingRule.NaicsCodes = rule.NaicsCodes != null ? string.Join(";", rule.NaicsCodes) : null;
        existingRule.States = rule.States != null ? string.Join(";", rule.States) : null;
        existingRule.Carrier = rule.Carrier;
        existingRule.Product = rule.Product;
        existingRule.Restrictions = rule.Restrictions != null ? string.Join(";", rule.Restrictions) : null;
        existingRule.Priority = rule.Priority;
        existingRule.Outcome = rule.Outcome;
        existingRule.RuleVersion = rule.RuleVersion;
        existingRule.Status = rule.Status;
        existingRule.EffectiveFrom = rule.EffectiveFrom;
        existingRule.EffectiveTo = rule.EffectiveTo;
        existingRule.MinRevenue = rule.MinRevenue;
        existingRule.MaxRevenue = rule.MaxRevenue;
        existingRule.MinYearsInBusiness = rule.MinYearsInBusiness;
        existingRule.MaxYearsInBusiness = rule.MaxYearsInBusiness;
        existingRule.PriorClaimsAllowed = rule.PriorClaimsAllowed;
        existingRule.Conditions = rule.Conditions != null ? string.Join(";", rule.Conditions) : null;
        existingRule.ContactEmail = rule.ContactEmail;
        existingRule.CreatedBy = rule.CreatedBy;
        existingRule.UpdatedAt = DateTime.UtcNow;
        existingRule.AdditionalJson = rule.AdditionalJson;
        
        await _context.SaveChangesAsync();
        
        return new RuleDetails(existingRule.RuleId, existingRule.Title, existingRule.Description, existingRule.BusinessType,
            existingRule.NaicsCodes?.Split(';'), existingRule.States?.Split(';'), existingRule.Carrier, existingRule.Product,
            existingRule.Restrictions?.Split(';'), existingRule.Priority, existingRule.Outcome, existingRule.RuleVersion, existingRule.Status,
            existingRule.EffectiveFrom, existingRule.EffectiveTo, existingRule.MinRevenue, existingRule.MaxRevenue,
            existingRule.MinYearsInBusiness, existingRule.MaxYearsInBusiness, existingRule.PriorClaimsAllowed,
            existingRule.Conditions?.Split(';'), existingRule.ContactEmail, existingRule.CreatedBy, existingRule.CreatedAt, existingRule.UpdatedAt, existingRule.AdditionalJson);
    }

    public async Task DeleteRuleAsync(string id)
    {
        var existingRule = await _context.Rules.FirstOrDefaultAsync(r => r.RuleId == id);
        if (existingRule == null)
            throw new KeyNotFoundException($"Rule {id} not found");
        
        _context.Rules.Remove(existingRule);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCarrierDetailsAsync(string id)
    {
        var existingCarrier = await _context.Carriers.FirstOrDefaultAsync(c => c.CarrierId == id);
        if (existingCarrier == null)
            throw new KeyNotFoundException($"Carrier {id} not found");
        
        _context.Carriers.Remove(existingCarrier);
        await _context.SaveChangesAsync();
    }

}