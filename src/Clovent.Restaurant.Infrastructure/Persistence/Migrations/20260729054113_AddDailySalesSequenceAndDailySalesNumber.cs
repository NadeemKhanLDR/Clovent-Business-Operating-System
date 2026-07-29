using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailySalesSequenceAndDailySalesNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailySalesNumber",
                schema: "Restaurant",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailySalesSequences",
                schema: "Restaurant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySalesSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailySalesSequences_WarehouseId_Date",
                schema: "Restaurant",
                table: "DailySalesSequences",
                columns: new[] { "WarehouseId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailySalesSequences",
                schema: "Restaurant");

            migrationBuilder.DropColumn(
                name: "DailySalesNumber",
                schema: "Restaurant",
                table: "Orders");
        }
    }
}
