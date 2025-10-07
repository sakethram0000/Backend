using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.NamingConventions;
using MyWebApi.Models;

namespace MyWebApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DbUser> Users { get; set; }
    public DbSet<DbProduct> Products { get; set; }
    public DbSet<DbRule> Rules { get; set; }
    public DbSet<DbCarrier> Carriers { get; set; }
    public DbSet<DbEvent> Events { get; set; }
    public DbSet<DbSubmission> Submissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // When using PostgreSQL, prefer snake_case lower-cased identifiers to match existing DBs
        modelBuilder.UseSnakeCaseNamingConvention();

        modelBuilder.Entity<DbUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(450).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Roles).HasMaxLength(200);
            entity.Property(e => e.OrganizationId).HasMaxLength(450);
            entity.Property(e => e.OrganizationName).HasMaxLength(200);
            entity.Property(e => e.AuthProvider).HasMaxLength(50);
            entity.Property(e => e.PasswordResetToken).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.FailedLoginAttempts).HasDefaultValue(0);
            
            // Indexes
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IDX_Users_Email");
            entity.HasIndex(e => e.PasswordResetToken).HasDatabaseName("IDX_Users_ResetToken");
        });

        modelBuilder.Entity<DbCarrier>(entity =>
        {
            entity.HasKey(e => e.CarrierId);
            entity.Property(e => e.CarrierId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.LegalName).HasMaxLength(300).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Country).HasMaxLength(2);
            entity.Property(e => e.PrimaryContactName).HasMaxLength(200);
            entity.Property(e => e.PrimaryContactEmail).HasMaxLength(255);
            entity.Property(e => e.PrimaryContactPhone).HasMaxLength(50);
            entity.Property(e => e.TechnicalContactName).HasMaxLength(200);
            entity.Property(e => e.TechnicalContactEmail).HasMaxLength(255);
            entity.Property(e => e.AuthMethod).HasMaxLength(50);
            entity.Property(e => e.SsoMetadataUrl).HasMaxLength(1000);
            entity.Property(e => e.ApiClientId).HasMaxLength(200);
            entity.Property(e => e.ApiSecretKeyRef).HasMaxLength(200);
            entity.Property(e => e.DataResidency).HasMaxLength(100);
            entity.Property(e => e.RuleUploadMethod).HasMaxLength(50);
            entity.Property(e => e.PreferredNaicsSource).HasMaxLength(50);
            entity.Property(e => e.PasWebhookUrl).HasMaxLength(1000);
            entity.Property(e => e.WebhookAuthType).HasMaxLength(50);
            entity.Property(e => e.WebhookSecretRef).HasMaxLength(200);
            entity.Property(e => e.ContractRef).HasMaxLength(200);
            entity.Property(e => e.BillingContactEmail).HasMaxLength(255);
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.RuleUploadAllowed).HasDefaultValue(false);
            entity.Property(e => e.RuleApprovalRequired).HasDefaultValue(true);
            entity.Property(e => e.DefaultRuleVersioning).HasDefaultValue(true);
            entity.Property(e => e.UseNaicsEnrichment).HasDefaultValue(false);
            
            // Indexes
            entity.HasIndex(e => e.DisplayName).HasDatabaseName("IDX_Carriers_DisplayName");
            entity.HasIndex(e => e.PrimaryContactEmail).HasDatabaseName("IDX_Carriers_PrimaryContactEmail");
        });

        modelBuilder.Entity<DbRule>(entity =>
        {
            entity.HasKey(e => e.RuleId);
            entity.Property(e => e.RuleId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BusinessType).HasMaxLength(100);
            entity.Property(e => e.Carrier).HasMaxLength(200);
            entity.Property(e => e.Product).HasMaxLength(200);
            entity.Property(e => e.Priority).HasMaxLength(50);
            entity.Property(e => e.Outcome).HasMaxLength(50);
            entity.Property(e => e.RuleVersion).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.ContactEmail).HasMaxLength(255);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.MinRevenue).HasPrecision(18, 2);
            entity.Property(e => e.MaxRevenue).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            
            // Indexes
            entity.HasIndex(e => new { e.Carrier, e.Product }).HasDatabaseName("IDX_Rules_Carrier_Product");
            entity.HasIndex(e => e.NaicsCodes).HasDatabaseName("IDX_Rules_Naics");
            entity.HasIndex(e => new { e.Status, e.EffectiveFrom, e.EffectiveTo }).HasDatabaseName("IDX_Rules_Status_Eff");
        });
    }
}