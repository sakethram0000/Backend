using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWebApi.Migrations
{
    /// <inheritdoc />
    public partial class CreateCarriersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carriers",
                columns: table => new
                {
                    CarrierId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    HeadquartersAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrimaryContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PrimaryContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TechnicalContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TechnicalContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AuthMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SsoMetadataUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApiClientId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApiSecretKeyRef = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DataResidency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductsOffered = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RuleUploadAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RuleUploadMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RuleApprovalRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DefaultRuleVersioning = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UseNaicsEnrichment = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PreferredNaicsSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PasWebhookUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WebhookAuthType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WebhookSecretRef = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContractRef = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BillingContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RetentionPolicyDays = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdditionalJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carriers", x => x.CarrierId);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_Carriers_DisplayName",
                table: "Carriers",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IDX_Carriers_PrimaryContactEmail",
                table: "Carriers",
                column: "PrimaryContactEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Carriers");
        }
    }
}
