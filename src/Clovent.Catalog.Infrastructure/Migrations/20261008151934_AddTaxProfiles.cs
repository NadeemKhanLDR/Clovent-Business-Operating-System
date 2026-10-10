using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxProfiles",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Authority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Jurisdiction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxClassification = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ItemClassification = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RatePercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PricingMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ReferenceDocument = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PaymentMethodRestriction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxProfiles_Code",
                schema: "Catalog",
                table: "TaxProfiles",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxProfiles",
                schema: "Catalog");
        }
    }
}
