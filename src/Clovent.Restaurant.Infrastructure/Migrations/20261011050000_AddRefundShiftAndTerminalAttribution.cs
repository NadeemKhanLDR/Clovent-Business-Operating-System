using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundShiftAndTerminalAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("""
                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[Refunds]'))
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Refunds]') AND name = 'ShiftId')
                        BEGIN
                            ALTER TABLE [Restaurant].[Refunds] ADD [ShiftId] uniqueidentifier NULL;
                        END

                        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Refunds]') AND name = 'TerminalId')
                        BEGIN
                            ALTER TABLE [Restaurant].[Refunds] ADD [TerminalId] uniqueidentifier NULL;
                        END

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Refunds_ShiftId' AND object_id = OBJECT_ID(N'[Restaurant].[Refunds]'))
                        BEGIN
                            CREATE INDEX [IX_Refunds_ShiftId] ON [Restaurant].[Refunds] ([ShiftId]);
                        END

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Refunds_TerminalId' AND object_id = OBJECT_ID(N'[Restaurant].[Refunds]'))
                        BEGIN
                            CREATE INDEX [IX_Refunds_TerminalId] ON [Restaurant].[Refunds] ([TerminalId]);
                        END
                    END
                    """);
            }
            else
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "ShiftId",
                    schema: "Restaurant",
                    table: "Refunds",
                    type: "uniqueidentifier",
                    nullable: true);

                migrationBuilder.AddColumn<Guid>(
                    name: "TerminalId",
                    schema: "Restaurant",
                    table: "Refunds",
                    type: "uniqueidentifier",
                    nullable: true);

                migrationBuilder.CreateIndex(
                    name: "IX_Refunds_ShiftId",
                    schema: "Restaurant",
                    table: "Refunds",
                    column: "ShiftId");

                migrationBuilder.CreateIndex(
                    name: "IX_Refunds_TerminalId",
                    schema: "Restaurant",
                    table: "Refunds",
                    column: "TerminalId");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("""
                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[Refunds]'))
                    BEGIN
                        IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Refunds_ShiftId' AND object_id = OBJECT_ID(N'[Restaurant].[Refunds]'))
                        BEGIN
                            DROP INDEX [IX_Refunds_ShiftId] ON [Restaurant].[Refunds];
                        END

                        IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Refunds_TerminalId' AND object_id = OBJECT_ID(N'[Restaurant].[Refunds]'))
                        BEGIN
                            DROP INDEX [IX_Refunds_TerminalId] ON [Restaurant].[Refunds];
                        END

                        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Refunds]') AND name = 'ShiftId')
                        BEGIN
                            ALTER TABLE [Restaurant].[Refunds] DROP COLUMN [ShiftId];
                        END

                        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Refunds]') AND name = 'TerminalId')
                        BEGIN
                            ALTER TABLE [Restaurant].[Refunds] DROP COLUMN [TerminalId];
                        END
                    END
                    """);
            }
            else
            {
                migrationBuilder.DropIndex(
                    name: "IX_Refunds_ShiftId",
                    schema: "Restaurant",
                    table: "Refunds");

                migrationBuilder.DropIndex(
                    name: "IX_Refunds_TerminalId",
                    schema: "Restaurant",
                    table: "Refunds");

                migrationBuilder.DropColumn(
                    name: "ShiftId",
                    schema: "Restaurant",
                    table: "Refunds");

                migrationBuilder.DropColumn(
                    name: "TerminalId",
                    schema: "Restaurant",
                    table: "Refunds");
            }
        }
    }
}
