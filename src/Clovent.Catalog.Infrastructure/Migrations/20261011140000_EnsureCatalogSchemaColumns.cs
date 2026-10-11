using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Catalog.Infrastructure.Migrations
{
    /// <summary>
    /// Additive forward migration reconciling previously shipped Catalog schema variants:
    /// - Variant A (commit 426be69): 20261008151934_AddTaxProfiles created [Catalog].[TaxProfiles] without [Catalog].[ProductCategories].[DefaultTaxProfileId].
    /// - Variant B (commit 4b1ef15): 20261008151934_AddTaxProfiles added [DefaultTaxProfileId] on fresh installs while [Products].[ItemType] and [ProductVariants].[IsAvailable] were added only via CatalogPersistenceInitializer.
    /// Preserves historical migration 20261008151934_AddTaxProfiles untouched and ensures all three columns exist idempotently across both upgrade lineages and fresh installations.
    /// </summary>
    public partial class EnsureCatalogSchemaColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("""
                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Catalog].[Products]'))
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID(N'[Catalog].[Products]')
                            AND name = N'ItemType'
                        )
                        BEGIN
                            ALTER TABLE [Catalog].[Products] ADD [ItemType] nvarchar(30) NOT NULL CONSTRAINT [DF_Products_ItemType] DEFAULT N'Prepared';
                        END
                    END

                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Catalog].[ProductVariants]'))
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID(N'[Catalog].[ProductVariants]')
                            AND name = N'IsAvailable'
                        )
                        BEGIN
                            ALTER TABLE [Catalog].[ProductVariants] ADD [IsAvailable] bit NOT NULL CONSTRAINT [DF_ProductVariants_IsAvailable] DEFAULT 1;
                        END
                    END

                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Catalog].[ProductCategories]'))
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID(N'[Catalog].[ProductCategories]')
                            AND name = N'DefaultTaxProfileId'
                        )
                        BEGIN
                            ALTER TABLE [Catalog].[ProductCategories] ADD [DefaultTaxProfileId] uniqueidentifier NULL;
                        END
                    END
                    """);
            }
            else
            {
                migrationBuilder.AddColumn<string>(
                    name: "ItemType",
                    schema: "Catalog",
                    table: "Products",
                    type: "nvarchar(30)",
                    maxLength: 30,
                    nullable: false,
                    defaultValue: "Prepared");

                migrationBuilder.AddColumn<bool>(
                    name: "IsAvailable",
                    schema: "Catalog",
                    table: "ProductVariants",
                    type: "bit",
                    nullable: false,
                    defaultValue: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("""
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Catalog].[ProductCategories]') AND name = N'DefaultTaxProfileId')
                    BEGIN
                        ALTER TABLE [Catalog].[ProductCategories] DROP COLUMN [DefaultTaxProfileId];
                    END
                    """);
            }
            else
            {
                migrationBuilder.DropColumn(
                    name: "IsAvailable",
                    schema: "Catalog",
                    table: "ProductVariants");

                migrationBuilder.DropColumn(
                    name: "ItemType",
                    schema: "Catalog",
                    table: "Products");
            }
        }
    }
}
