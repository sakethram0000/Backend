using Microsoft.EntityFrameworkCore;
using MyWebApi.Models;
using MyWebApi.Data;

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
        modelBuilder.Entity<DbUser>(entity =>
        {
            // Map to lowercase table names in Postgres (unquoted identifiers are lowercase)
            entity.ToTable("users");
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
            entity.Property(e => e.CreatedAt).HasColumnType("text").HasConversion<TextDateTimeConverter>();
            entity.Property(e => e.LastLoginAt).HasColumnType("text").HasConversion<TextNullableDateTimeConverter>();
            entity.Property(e => e.LockoutEnd).HasColumnType("text").HasConversion<TextNullableDateTimeConverter>();
            entity.Property(e => e.PasswordResetExpiry).HasColumnType("text").HasConversion<TextNullableDateTimeConverter>();
            entity.Property(e => e.IsActive).HasColumnType("boolean").HasDefaultValue(true);
            entity.Property(e => e.FailedLoginAttempts).HasDefaultValue(0);
            
            // Indexes
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IDX_Users_Email");
            entity.HasIndex(e => e.PasswordResetToken).HasDatabaseName("IDX_Users_ResetToken");
        });

        modelBuilder.Entity<DbCarrier>(entity =>
        {
            entity.ToTable("carriers");
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
            entity.Property(e => e.CreatedAt).HasColumnType("text").HasConversion<TextDateTimeConverter>();
            entity.Property(e => e.UpdatedAt).HasColumnType("text").HasConversion<TextNullableDateTimeConverter>();
            entity.Property(e => e.RuleUploadAllowed).HasColumnType("boolean").HasDefaultValue(false);
            entity.Property(e => e.RuleApprovalRequired).HasColumnType("boolean").HasDefaultValue(true);
            entity.Property(e => e.DefaultRuleVersioning).HasColumnType("boolean").HasDefaultValue(true);
            entity.Property(e => e.UseNaicsEnrichment).HasColumnType("boolean").HasDefaultValue(false);
            
            // Indexes
            entity.HasIndex(e => e.DisplayName).HasDatabaseName("IDX_Carriers_DisplayName");
            entity.HasIndex(e => e.PrimaryContactEmail).HasDatabaseName("IDX_Carriers_PrimaryContactEmail");
        });

        modelBuilder.Entity<DbRule>(entity =>
        {
            entity.ToTable("rules");
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
            entity.Property(e => e.CreatedAt).HasColumnType("text").HasConversion<TextDateTimeConverter>();
            entity.Property(e => e.UpdatedAt).HasColumnType("text").HasConversion<TextNullableDateTimeConverter>();
            entity.Property(e => e.EffectiveFrom).HasColumnType("text").HasConversion<TextNullableDateTimeConverter>();
            entity.Property(e => e.EffectiveTo).HasColumnType("text").HasConversion<TextNullableDateTimeConverter>();
            
            // Indexes
            entity.HasIndex(e => new { e.Carrier, e.Product }).HasDatabaseName("IDX_Rules_Carrier_Product");
            entity.HasIndex(e => e.NaicsCodes).HasDatabaseName("IDX_Rules_Naics");
            entity.HasIndex(e => new { e.Status, e.EffectiveFrom, e.EffectiveTo }).HasDatabaseName("IDX_Rules_Status_Eff");
        });
        // Configure other entities
        modelBuilder.Entity<DbProduct>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasColumnType("text").HasConversion<TextDateTimeConverter>();
        });
        
        modelBuilder.Entity<DbEvent>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(e => e.EventId);
        });
        
        modelBuilder.Entity<DbSubmission>(entity =>
        {
            entity.ToTable("submissions");
            entity.HasKey(e => e.SubmissionId);
        });
    }
}