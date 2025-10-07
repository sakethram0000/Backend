using System;
using MyWebApi.Services;
using MyWebApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Resolve connection string: prefer environment variable, then appsettings, then a hardcoded fallback for quick testing.
var envConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
var configConn = builder.Configuration.GetConnectionString("DefaultConnection");
// TODO: Replace the hardcoded fallback below with a real test connection string if needed.
// TEMPORARY TEST CONNECTION: hardcoded Retool Postgres URL (remove before committing to a public repo)
var hardcodedFallbackConn = "postgresql://retool:npg_Bwa4hd0bcozm@ep-wispy-math-af5ww262.c-2.us-west-2.retooldb.com/retool?sslmode=require";
var connectionString = !string.IsNullOrWhiteSpace(envConn) ? envConn : (!string.IsNullOrWhiteSpace(configConn) ? configConn : hardcodedFallbackConn);

// Detect Postgres-style URLs and use Npgsql provider when appropriate.
var npgsqlConn = connectionString;
if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) || connectionString.Contains("Host=") || connectionString.Contains("Port="))
{
    // If the connection string is a URI (postgres://user:pass@host:port/dbname), convert to Npgsql-compatible connection string.
    if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var builderN = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = userInfo.Length > 0 ? userInfo[0] : string.Empty,
            Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = Npgsql.SslMode.Require
            // Do NOT set TrustServerCertificate here (obsolete). Configure certificate/trust properly in your environment.
        };
        npgsqlConn = builderN.ToString();
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(npgsqlConn));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddControllers();
builder.Services.AddScoped<ICheckerService, CheckerService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ICanvasService, CanvasService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IIdGenerationService, IdGenerationService>();
builder.Services.AddScoped<SeedService>();

// Add JWT Authentication
// Prefer environment variables for secrets during testing; fall back to appsettings.json, then to hardcoded defaults.
var jwtSection = builder.Configuration.GetSection("Jwt");
var envSecret = Environment.GetEnvironmentVariable("Jwt__SecretKey");
var envIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer");
var envAudience = Environment.GetEnvironmentVariable("Jwt__Audience");
var envExpiration = Environment.GetEnvironmentVariable("Jwt__ExpirationMinutes");

var secretKey = !string.IsNullOrWhiteSpace(envSecret) ? envSecret : (jwtSection["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLongForProduction!");
var issuer = !string.IsNullOrWhiteSpace(envIssuer) ? envIssuer : (jwtSection["Issuer"] ?? "AppetiteChecker");
var audience = !string.IsNullOrWhiteSpace(envAudience) ? envAudience : (jwtSection["Audience"] ?? "AppetiteCheckerUsers");
var expirationMinutes = !string.IsNullOrWhiteSpace(envExpiration) ? envExpiration : (jwtSection["ExpirationMinutes"] ?? "60");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Add CORS - read allowed origins from configuration (appsettings / environment) with a fallback
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (configuredOrigins == null || configuredOrigins.Length == 0)
{
    configuredOrigins = new[]
    {
        "http://localhost:3000",
        "http://localhost:3001",
        "http://localhost:3002",
        "https://frontend-woad-three-37.vercel.app"
    };
}

// Optional: allow credentials (cookies) from configured origins. Default: false.
var allowCorsCredentials = builder.Configuration.GetValue<bool?>("Cors:AllowCredentials") ?? false;

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(configuredOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();

        if (allowCorsCredentials)
        {
            // If the frontend needs to send cookies/credentials (fetch credentials: 'include'),
            // enable AllowCredentials. This must not be used with AllowAnyOrigin.
            policy.AllowCredentials();
        }
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Enable HTTPS redirection during development where it's configured
    app.UseHttpsRedirection();
}
else
{
    // In production (e.g., Render) HTTPS may be handled by the platform/load balancer.
    // Avoid forcing HTTPS redirection here to prevent redirect/port detection warnings.
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Lightweight root and health endpoints so the service responds at '/'
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "MyWebApi", env = app.Environment.EnvironmentName }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
    await seedService.SeedInitialDataAsync();
}

app.Run();
