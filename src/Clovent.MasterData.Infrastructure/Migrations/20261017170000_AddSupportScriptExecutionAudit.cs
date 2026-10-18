using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportScriptExecutionAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "MasterData");

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = 'SupportScriptExecutionAudit')
                BEGIN
                    CREATE TABLE [MasterData].[SupportScriptExecutionAudit] (
                        [Id] BIGINT IDENTITY(1,1) NOT NULL,
                        [ScriptId] NVARCHAR(100) NOT NULL,
                        [ScriptHash] VARCHAR(64) NOT NULL,
                        [Operator] NVARCHAR(100) NOT NULL,
                        [ExecutedAtUtc] DATETIME2 NOT NULL,
                        [TargetDatabase] NVARCHAR(128) NOT NULL,
                        [PreCheckSummary] NVARCHAR(MAX) NULL,
                        [AffectedRows] INT NOT NULL,
                        [Outcome] NVARCHAR(50) NOT NULL,
                        [ErrorMessage] NVARCHAR(MAX) NULL,
                        [CompensationReference] NVARCHAR(200) NULL,
                        CONSTRAINT [PK_SupportScriptExecutionAudit] PRIMARY KEY CLUSTERED ([Id] ASC)
                    );
                END
            """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupportScriptExecutionAudit_ScriptId' AND object_id = OBJECT_ID(N'[MasterData].[SupportScriptExecutionAudit]'))
                BEGIN
                    CREATE NONCLUSTERED INDEX [IX_SupportScriptExecutionAudit_ScriptId] ON [MasterData].[SupportScriptExecutionAudit] ([ScriptId]);
                END
            """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SupportScriptExecutionAudit_ScriptId_Success' AND object_id = OBJECT_ID(N'[MasterData].[SupportScriptExecutionAudit]'))
                BEGIN
                    CREATE UNIQUE NONCLUSTERED INDEX [UX_SupportScriptExecutionAudit_ScriptId_Success]
                    ON [MasterData].[SupportScriptExecutionAudit] ([ScriptId])
                    WHERE [Outcome] = 'Success';
                END
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportScriptExecutionAudit",
                schema: "MasterData");
        }
    }
}
