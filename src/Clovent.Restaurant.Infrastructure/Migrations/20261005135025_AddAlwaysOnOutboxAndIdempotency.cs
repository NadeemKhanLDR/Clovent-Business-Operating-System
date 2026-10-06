using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlwaysOnOutboxAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Payments]') AND name = 'IdempotencyKey')
                    ALTER TABLE [Restaurant].[Payments] ADD [IdempotencyKey] nvarchar(200) NULL;
            """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Orders]') AND name = 'ReceiptSnapshotJson')
                    ALTER TABLE [Restaurant].[Orders] ADD [ReceiptSnapshotJson] nvarchar(max) NULL;
            """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[OutboxMessages]'))
                BEGIN
                    CREATE TABLE [Restaurant].[OutboxMessages] (
                        [Id] uniqueidentifier NOT NULL,
                        [MessageType] nvarchar(100) NOT NULL,
                        [AggregateType] nvarchar(100) NOT NULL,
                        [AggregateId] nvarchar(100) NOT NULL,
                        [CorrelationId] nvarchar(100) NOT NULL,
                        [IdempotencyKey] nvarchar(200) NULL,
                        [Payload] nvarchar(max) NOT NULL,
                        [Status] nvarchar(50) NOT NULL,
                        [AttemptCount] int NOT NULL,
                        [CreatedAtUtc] datetimeoffset NOT NULL,
                        [AvailableAtUtc] datetimeoffset NOT NULL,
                        [ProcessingStartedAtUtc] datetimeoffset NULL,
                        [CompletedAtUtc] datetimeoffset NULL,
                        [LastAttemptAtUtc] datetimeoffset NULL,
                        [LastError] nvarchar(4000) NULL,
                        [NextRetryAtUtc] datetimeoffset NULL,
                        [Priority] int NOT NULL,
                        [Version] varbinary(max) NOT NULL,
                        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
                    );
                END
            """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_IdempotencyKey' AND object_id = OBJECT_ID(N'[Restaurant].[Payments]'))
                    EXEC(N'CREATE UNIQUE INDEX [IX_Payments_IdempotencyKey] ON [Restaurant].[Payments] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
            """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OutboxMessages_CorrelationId' AND object_id = OBJECT_ID(N'[Restaurant].[OutboxMessages]'))
                    CREATE INDEX [IX_OutboxMessages_CorrelationId] ON [Restaurant].[OutboxMessages] ([CorrelationId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OutboxMessages_IdempotencyKey' AND object_id = OBJECT_ID(N'[Restaurant].[OutboxMessages]'))
                    CREATE UNIQUE INDEX [IX_OutboxMessages_IdempotencyKey] ON [Restaurant].[OutboxMessages] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OutboxMessages_Status_Available_Priority' AND object_id = OBJECT_ID(N'[Restaurant].[OutboxMessages]'))
                    CREATE INDEX [IX_OutboxMessages_Status_Available_Priority] ON [Restaurant].[OutboxMessages] ([Status], [AvailableAtUtc], [Priority]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OutboxMessages_Type_Status' AND object_id = OBJECT_ID(N'[Restaurant].[OutboxMessages]'))
                    CREATE INDEX [IX_OutboxMessages_Type_Status] ON [Restaurant].[OutboxMessages] ([MessageType], [Status]);
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "Restaurant");

            migrationBuilder.DropIndex(
                name: "IX_Payments_IdempotencyKey",
                schema: "Restaurant",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "Restaurant",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReceiptSnapshotJson",
                schema: "Restaurant",
                table: "Orders");
        }
    }
}
