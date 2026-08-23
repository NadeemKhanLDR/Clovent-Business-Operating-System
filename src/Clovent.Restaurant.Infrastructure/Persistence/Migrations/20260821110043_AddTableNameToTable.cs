using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTableNameToTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "Restaurant",
                table: "Tables",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE [Restaurant].[Tables] SET [Name] = [Code];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                schema: "Restaurant",
                table: "Tables");
        }
    }
}
