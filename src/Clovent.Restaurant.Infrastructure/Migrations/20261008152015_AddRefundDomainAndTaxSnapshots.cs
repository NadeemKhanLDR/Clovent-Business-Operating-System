using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundDomainAndTaxSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedDiscount",
                schema: "Restaurant",
                table: "OrderLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<string>(
                name: "CalculationPolicyVersion",
                schema: "Restaurant",
                table: "OrderLines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "Restaurant",
                table: "OrderLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxAuthority",
                schema: "Restaurant",
                table: "OrderLines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxClassification",
                schema: "Restaurant",
                table: "OrderLines",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                schema: "Restaurant",
                table: "OrderLines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableBase",
                schema: "Restaurant",
                table: "OrderLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Refunds",
                schema: "Restaurant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubtotalRefunded = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountReversedTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxReversedTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotalRefunded = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SettlementMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SettlementReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefundLines",
                schema: "Restaurant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountReversed = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxReversed = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotalRefunded = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxClassification = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxRatePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxIsInclusive = table.Column<bool>(type: "bit", nullable: false),
                    InventoryDisposition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundLines_Refunds_RefundId",
                        column: x => x.RefundId,
                        principalSchema: "Restaurant",
                        principalTable: "Refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefundLines_OrderLineId",
                schema: "Restaurant",
                table: "RefundLines",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundLines_RefundId",
                schema: "Restaurant",
                table: "RefundLines",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_BranchId",
                schema: "Restaurant",
                table: "Refunds",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_IdempotencyKey",
                schema: "Restaurant",
                table: "Refunds",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_OrderId",
                schema: "Restaurant",
                table: "Refunds",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RefundedAtUtc",
                schema: "Restaurant",
                table: "Refunds",
                column: "RefundedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RefundNumber",
                schema: "Restaurant",
                table: "Refunds",
                column: "RefundNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefundLines",
                schema: "Restaurant");

            migrationBuilder.DropTable(
                name: "Refunds",
                schema: "Restaurant");

            migrationBuilder.DropColumn(
                name: "AllocatedDiscount",
                schema: "Restaurant",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "CalculationPolicyVersion",
                schema: "Restaurant",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "Restaurant",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "TaxAuthority",
                schema: "Restaurant",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "TaxClassification",
                schema: "Restaurant",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                schema: "Restaurant",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "TaxableBase",
                schema: "Restaurant",
                table: "OrderLines");
        }
    }
}
