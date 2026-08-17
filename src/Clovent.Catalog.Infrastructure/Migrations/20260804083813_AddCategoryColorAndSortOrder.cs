using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryColorAndSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                schema: "Catalog",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                schema: "Catalog",
                table: "ProductCategories",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                schema: "Catalog",
                table: "ProductCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                schema: "Catalog",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                schema: "Catalog",
                table: "ProductCategories");
        }
    }
}
