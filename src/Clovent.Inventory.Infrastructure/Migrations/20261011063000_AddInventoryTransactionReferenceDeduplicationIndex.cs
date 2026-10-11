using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTransactionReferenceDeduplicationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("""
                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Inventory].[InventoryTransactions]'))
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM sys.indexes
                            WHERE name = N'IX_InventoryTransactions_Reference_Deduplication'
                            AND object_id = OBJECT_ID(N'[Inventory].[InventoryTransactions]')
                        )
                        BEGIN
                            CREATE UNIQUE INDEX [IX_InventoryTransactions_Reference_Deduplication]
                            ON [Inventory].[InventoryTransactions] ([ReferenceType], [ReferenceId], [WarehouseId], [ProductVariantId])
                            WHERE [ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL;
                        END
                    END
                    """);
            }
            else
            {
                migrationBuilder.CreateIndex(
                    name: "IX_InventoryTransactions_Reference_Deduplication",
                    schema: "Inventory",
                    table: "InventoryTransactions",
                    columns: new[] { "ReferenceType", "ReferenceId", "WarehouseId", "ProductVariantId" },
                    unique: true,
                    filter: "[ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("""
                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Inventory].[InventoryTransactions]'))
                    BEGIN
                        IF EXISTS (
                            SELECT 1 FROM sys.indexes
                            WHERE name = N'IX_InventoryTransactions_Reference_Deduplication'
                            AND object_id = OBJECT_ID(N'[Inventory].[InventoryTransactions]')
                        )
                        BEGIN
                            DROP INDEX [IX_InventoryTransactions_Reference_Deduplication] ON [Inventory].[InventoryTransactions];
                        END
                    END
                    """);
            }
            else
            {
                migrationBuilder.DropIndex(
                    name: "IX_InventoryTransactions_Reference_Deduplication",
                    schema: "Inventory",
                    table: "InventoryTransactions");
            }
        }
    }
}
