using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeAttendanceSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceSessions",
                schema: "Restaurant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PunchInTerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PunchOutTerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PunchInAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PunchOutAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_BranchId",
                schema: "Restaurant",
                table: "AttendanceSessions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_PunchInAtUtc",
                schema: "Restaurant",
                table: "AttendanceSessions",
                column: "PunchInAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_Status",
                schema: "Restaurant",
                table: "AttendanceSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_UserId",
                schema: "Restaurant",
                table: "AttendanceSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_UserId_Open",
                schema: "Restaurant",
                table: "AttendanceSessions",
                column: "UserId",
                unique: true,
                filter: "[PunchOutAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceSessions",
                schema: "Restaurant");
        }
    }
}
