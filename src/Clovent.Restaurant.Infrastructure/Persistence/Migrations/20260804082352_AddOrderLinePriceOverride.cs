using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderLinePriceOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPriceOverridden",
                schema: "Restaurant",
                table: "OrderLines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                schema: "Restaurant",
                table: "OrderLines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PriceOverriddenAtUtc",
                schema: "Restaurant",
                table: "OrderLines",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceOverriddenBy",
                schema: "Restaurant",
                table: "OrderLines",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceOverrideReason",
                schema: "Restaurant",
                table: "OrderLines",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Every OrderLine that already existed before this migration was
            // never overridden - its OriginalUnitPrice is simply its current
            // UnitPrice, not the column's 0 default (which would otherwise
            // read as "originally free" for every pre-existing line).
            migrationBuilder.Sql("UPDATE [Restaurant].[OrderLines] SET [OriginalUnitPrice] = [UnitPrice];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPriceOverridden",
                schema: "Restaurant",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                schema: "Restaurant",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "PriceOverriddenAtUtc",
                schema: "Restaurant",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "PriceOverriddenBy",
                schema: "Restaurant",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "PriceOverrideReason",
                schema: "Restaurant",
                table: "OrderLines");
        }
    }
}
