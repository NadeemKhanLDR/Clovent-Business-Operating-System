using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartPosFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuickOrderTemplates",
                schema: "Restaurant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickOrderTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationRules",
                schema: "Restaurant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecommendedVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    DaysOfWeek = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuickOrderTemplateItems",
                schema: "Restaurant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    TemplateUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickOrderTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickOrderTemplateItems_QuickOrderTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "Restaurant",
                        principalTable: "QuickOrderTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuickOrderTemplateItems_TemplateId",
                schema: "Restaurant",
                table: "QuickOrderTemplateItems",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickOrderTemplates_IsActive",
                schema: "Restaurant",
                table: "QuickOrderTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationRules_IsActive",
                schema: "Restaurant",
                table: "RecommendationRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationRules_Priority",
                schema: "Restaurant",
                table: "RecommendationRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationRules_ProductId",
                schema: "Restaurant",
                table: "RecommendationRules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationRules_RecommendedVariantId",
                schema: "Restaurant",
                table: "RecommendationRules",
                column: "RecommendedVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuickOrderTemplateItems",
                schema: "Restaurant");

            migrationBuilder.DropTable(
                name: "RecommendationRules",
                schema: "Restaurant");

            migrationBuilder.DropTable(
                name: "QuickOrderTemplates",
                schema: "Restaurant");
        }
    }
}
