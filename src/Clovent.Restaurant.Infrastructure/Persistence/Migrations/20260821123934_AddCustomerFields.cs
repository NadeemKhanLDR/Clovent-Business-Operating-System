using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Mobile2",
                schema: "Restaurant",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "Restaurant",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopNo",
                schema: "Restaurant",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mobile2",
                schema: "Restaurant",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "Restaurant",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShopNo",
                schema: "Restaurant",
                table: "Customers");
        }
    }
}
