using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSuggestionAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SuggestionEvents",
                schema: "Restaurant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    OrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcceptedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AcceptedUnitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionEvents_Kind",
                schema: "Restaurant",
                table: "SuggestionEvents",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionEvents_OccurredAtUtc",
                schema: "Restaurant",
                table: "SuggestionEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionEvents_OrderId",
                schema: "Restaurant",
                table: "SuggestionEvents",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionEvents_VariantId",
                schema: "Restaurant",
                table: "SuggestionEvents",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuggestionEvents",
                schema: "Restaurant");
        }
    }
}
