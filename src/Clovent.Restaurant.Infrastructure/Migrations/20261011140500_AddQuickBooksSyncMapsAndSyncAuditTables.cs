using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuickBooksSyncMapsAndSyncAuditTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("""
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[QuickBooksSyncMaps]'))
                    BEGIN
                        CREATE TABLE [Restaurant].[QuickBooksSyncMaps] (
                            [Id] uniqueidentifier NOT NULL,
                            [LocalEntityId] uniqueidentifier NOT NULL,
                            [EntityType] nvarchar(50) NOT NULL,
                            [QuickBooksTxnId] nvarchar(100) NULL,
                            [QuickBooksDocNumber] nvarchar(50) NULL,
                            [QuickBooksEditSequence] nvarchar(50) NULL,
                            [Amount] decimal(18,2) NOT NULL,
                            [Currency] nvarchar(10) NOT NULL,
                            [Status] nvarchar(30) NOT NULL,
                            [LastError] nvarchar(2000) NULL,
                            [RetryCount] int NOT NULL,
                            [CreatedAtUtc] datetimeoffset NOT NULL,
                            [SyncedAtUtc] datetimeoffset NULL,
                            [RequestPayloadJson] nvarchar(max) NULL,
                            [ResponsePayloadJson] nvarchar(max) NULL,
                            [BranchId] uniqueidentifier NULL,
                            [TerminalId] uniqueidentifier NULL,
                            [ManagerOverrideNotes] nvarchar(1000) NULL,
                            [ManagerOverrideUserId] uniqueidentifier NULL,
                            [ManagerOverrideAtUtc] datetimeoffset NULL,
                            CONSTRAINT [PK_QuickBooksSyncMaps] PRIMARY KEY ([Id])
                        );
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QuickBooksSyncMaps_EntityType_LocalEntityId' AND object_id = OBJECT_ID(N'[Restaurant].[QuickBooksSyncMaps]'))
                        CREATE UNIQUE INDEX [IX_QuickBooksSyncMaps_EntityType_LocalEntityId] ON [Restaurant].[QuickBooksSyncMaps] ([EntityType], [LocalEntityId]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QuickBooksSyncMaps_QuickBooksTxnId' AND object_id = OBJECT_ID(N'[Restaurant].[QuickBooksSyncMaps]'))
                        CREATE INDEX [IX_QuickBooksSyncMaps_QuickBooksTxnId] ON [Restaurant].[QuickBooksSyncMaps] ([QuickBooksTxnId]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QuickBooksSyncMaps_Status' AND object_id = OBJECT_ID(N'[Restaurant].[QuickBooksSyncMaps]'))
                        CREATE INDEX [IX_QuickBooksSyncMaps_Status] ON [Restaurant].[QuickBooksSyncMaps] ([Status]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QuickBooksSyncMaps_CreatedAtUtc' AND object_id = OBJECT_ID(N'[Restaurant].[QuickBooksSyncMaps]'))
                        CREATE INDEX [IX_QuickBooksSyncMaps_CreatedAtUtc] ON [Restaurant].[QuickBooksSyncMaps] ([CreatedAtUtc]);

                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[SyncInboxRecords]'))
                    BEGIN
                        CREATE TABLE [Restaurant].[SyncInboxRecords] (
                            [IdempotencyKey] nvarchar(200) NOT NULL,
                            [PacketId] uniqueidentifier NOT NULL,
                            [OrganizationId] uniqueidentifier NOT NULL,
                            [BranchId] uniqueidentifier NOT NULL,
                            [SourceTerminalId] uniqueidentifier NOT NULL,
                            [EntityKind] nvarchar(100) NOT NULL,
                            [EntityId] nvarchar(200) NOT NULL,
                            [SchemaVersion] int NOT NULL,
                            [PayloadHash] nvarchar(128) NOT NULL,
                            [PayloadJson] nvarchar(max) NOT NULL,
                            [Status] nvarchar(50) NOT NULL,
                            [ReceivedAtUtc] datetimeoffset NOT NULL,
                            [ProcessedAtUtc] datetimeoffset NULL,
                            [FailureReason] nvarchar(2000) NULL,
                            [StagedConflictId] uniqueidentifier NULL,
                            [ConcurrencyToken] nvarchar(200) NULL,
                            CONSTRAINT [PK_SyncInboxRecords] PRIMARY KEY ([IdempotencyKey])
                        );
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SyncInboxRecords_PacketId' AND object_id = OBJECT_ID(N'[Restaurant].[SyncInboxRecords]'))
                        CREATE INDEX [IX_SyncInboxRecords_PacketId] ON [Restaurant].[SyncInboxRecords] ([PacketId]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SyncInboxRecords_Status' AND object_id = OBJECT_ID(N'[Restaurant].[SyncInboxRecords]'))
                        CREATE INDEX [IX_SyncInboxRecords_Status] ON [Restaurant].[SyncInboxRecords] ([Status]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SyncInboxRecords_EntityKind_EntityId' AND object_id = OBJECT_ID(N'[Restaurant].[SyncInboxRecords]'))
                        CREATE INDEX [IX_SyncInboxRecords_EntityKind_EntityId] ON [Restaurant].[SyncInboxRecords] ([EntityKind], [EntityId]);

                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[SyncConflicts]'))
                    BEGIN
                        CREATE TABLE [Restaurant].[SyncConflicts] (
                            [ConflictId] uniqueidentifier NOT NULL,
                            [PacketId] uniqueidentifier NOT NULL,
                            [OriginalPacketJson] nvarchar(max) NOT NULL,
                            [EntityKind] nvarchar(100) NOT NULL,
                            [EntityId] nvarchar(200) NOT NULL,
                            [SourceBranchId] uniqueidentifier NOT NULL,
                            [SourceTerminalId] uniqueidentifier NOT NULL,
                            [IncomingValue] nvarchar(500) NOT NULL,
                            [CurrentValue] nvarchar(500) NOT NULL,
                            [IncomingConcurrencyToken] nvarchar(200) NULL,
                            [CurrentConcurrencyToken] nvarchar(200) NULL,
                            [ConflictType] nvarchar(50) NOT NULL,
                            [Status] nvarchar(50) NOT NULL,
                            [ConflictReason] nvarchar(1000) NOT NULL,
                            [DetectedAtUtc] datetimeoffset NOT NULL,
                            [ReviewedAtUtc] datetimeoffset NULL,
                            [ReviewedByUserId] uniqueidentifier NULL,
                            [ResolutionNotes] nvarchar(2000) NULL,
                            [ResultingEffect] nvarchar(1000) NULL,
                            CONSTRAINT [PK_SyncConflicts] PRIMARY KEY ([ConflictId])
                        );
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SyncConflicts_Status' AND object_id = OBJECT_ID(N'[Restaurant].[SyncConflicts]'))
                        CREATE INDEX [IX_SyncConflicts_Status] ON [Restaurant].[SyncConflicts] ([Status]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SyncConflicts_EntityKind_EntityId' AND object_id = OBJECT_ID(N'[Restaurant].[SyncConflicts]'))
                        CREATE INDEX [IX_SyncConflicts_EntityKind_EntityId] ON [Restaurant].[SyncConflicts] ([EntityKind], [EntityId]);

                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[TerminalShiftSyncSummaries]'))
                    BEGIN
                        CREATE TABLE [Restaurant].[TerminalShiftSyncSummaries] (
                            [Id] uniqueidentifier NOT NULL,
                            [BranchId] uniqueidentifier NOT NULL,
                            [TerminalId] uniqueidentifier NOT NULL,
                            [ShiftNumber] int NOT NULL,
                            [CashierId] uniqueidentifier NOT NULL,
                            [CashierName] nvarchar(200) NOT NULL,
                            [OpenedAtUtc] datetimeoffset NOT NULL,
                            [ClosedAtUtc] datetimeoffset NULL,
                            [StartingCash] decimal(18,4) NOT NULL,
                            [CountedCash] decimal(18,4) NOT NULL,
                            [ExpectedCash] decimal(18,4) NOT NULL,
                            [CashVariance] decimal(18,4) NOT NULL,
                            [NetSales] decimal(18,4) NOT NULL,
                            [TotalOrdersCount] int NOT NULL,
                            [TimestampUtc] datetimeoffset NOT NULL,
                            [ReplicatedAtUtc] datetimeoffset NOT NULL,
                            CONSTRAINT [PK_TerminalShiftSyncSummaries] PRIMARY KEY ([Id])
                        );
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TerminalShiftSyncSummaries_Branch_Terminal_Shift' AND object_id = OBJECT_ID(N'[Restaurant].[TerminalShiftSyncSummaries]'))
                        CREATE UNIQUE INDEX [IX_TerminalShiftSyncSummaries_Branch_Terminal_Shift] ON [Restaurant].[TerminalShiftSyncSummaries] ([BranchId], [TerminalId], [ShiftNumber]);

                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[CashierAuditAlerts]'))
                    BEGIN
                        CREATE TABLE [Restaurant].[CashierAuditAlerts] (
                            [Id] uniqueidentifier NOT NULL,
                            [ShiftId] uniqueidentifier NULL,
                            [OrderId] uniqueidentifier NULL,
                            [CashierId] uniqueidentifier NULL,
                            [CashierName] nvarchar(200) NOT NULL,
                            [AnomalyType] nvarchar(60) NOT NULL,
                            [Severity] nvarchar(30) NOT NULL,
                            [RiskScore] decimal(5,2) NOT NULL,
                            [Description] nvarchar(1000) NOT NULL,
                            [SuspectDetailsJson] nvarchar(max) NULL,
                            [DetectedAtUtc] datetimeoffset NOT NULL,
                            [Status] nvarchar(30) NOT NULL,
                            [ReviewedBy] nvarchar(200) NULL,
                            [ReviewedAtUtc] datetimeoffset NULL,
                            [ResolutionNotes] nvarchar(1000) NULL,
                            [CreatedAtUtc] datetimeoffset NOT NULL,
                            CONSTRAINT [PK_CashierAuditAlerts] PRIMARY KEY ([Id])
                        );
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CashierAuditAlerts_DetectedAtUtc' AND object_id = OBJECT_ID(N'[Restaurant].[CashierAuditAlerts]'))
                        CREATE INDEX [IX_CashierAuditAlerts_DetectedAtUtc] ON [Restaurant].[CashierAuditAlerts] ([DetectedAtUtc]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CashierAuditAlerts_CashierName' AND object_id = OBJECT_ID(N'[Restaurant].[CashierAuditAlerts]'))
                        CREATE INDEX [IX_CashierAuditAlerts_CashierName] ON [Restaurant].[CashierAuditAlerts] ([CashierName]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CashierAuditAlerts_Status' AND object_id = OBJECT_ID(N'[Restaurant].[CashierAuditAlerts]'))
                        CREATE INDEX [IX_CashierAuditAlerts_Status] ON [Restaurant].[CashierAuditAlerts] ([Status]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CashierAuditAlerts_AnomalyType' AND object_id = OBJECT_ID(N'[Restaurant].[CashierAuditAlerts]'))
                        CREATE INDEX [IX_CashierAuditAlerts_AnomalyType] ON [Restaurant].[CashierAuditAlerts] ([AnomalyType]);

                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[Tables]'))
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tables_DiningAreaId_Code' AND object_id = OBJECT_ID(N'[Restaurant].[Tables]'))
                        BEGIN
                            CREATE UNIQUE NONCLUSTERED INDEX [IX_Tables_DiningAreaId_Code]
                            ON [Restaurant].[Tables] ([DiningAreaId], [Code]);
                        END
                    END
                    """);
            }
            else
            {
                migrationBuilder.CreateTable(
                    name: "CashierAuditAlerts",
                    schema: "Restaurant",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                        OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                        CashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                        CashierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                        AnomalyType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                        Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                        RiskScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                        Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                        SuspectDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        DetectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                        ReviewedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                        ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                        CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_CashierAuditAlerts", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "QuickBooksSyncMaps",
                    schema: "Restaurant",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        LocalEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                        QuickBooksTxnId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                        QuickBooksDocNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                        QuickBooksEditSequence = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                        Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                        Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                        Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                        LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                        RetryCount = table.Column<int>(type: "int", nullable: false),
                        CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        SyncedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        RequestPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        ResponsePayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                        TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                        ManagerOverrideNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                        ManagerOverrideUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                        ManagerOverrideAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_QuickBooksSyncMaps", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "SyncConflicts",
                    schema: "Restaurant",
                    columns: table => new
                    {
                        ConflictId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        PacketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        OriginalPacketJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        EntityKind = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                        EntityId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                        SourceBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        SourceTerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        IncomingValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                        CurrentValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                        IncomingConcurrencyToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                        CurrentConcurrencyToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                        ConflictType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                        Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                        ConflictReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                        DetectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                        ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                        ResultingEffect = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_SyncConflicts", x => x.ConflictId);
                    });

                migrationBuilder.CreateTable(
                    name: "SyncInboxRecords",
                    schema: "Restaurant",
                    columns: table => new
                    {
                        IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                        PacketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        SourceTerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        EntityKind = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                        EntityId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                        SchemaVersion = table.Column<int>(type: "int", nullable: false),
                        PayloadHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                        PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                        Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                        ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                        StagedConflictId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                        ConcurrencyToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_SyncInboxRecords", x => x.IdempotencyKey);
                    });

                migrationBuilder.CreateTable(
                    name: "TerminalShiftSyncSummaries",
                    schema: "Restaurant",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        ShiftNumber = table.Column<int>(type: "int", nullable: false),
                        CashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        CashierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                        OpenedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        StartingCash = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                        CountedCash = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                        ExpectedCash = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                        CashVariance = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                        NetSales = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                        TotalOrdersCount = table.Column<int>(type: "int", nullable: false),
                        TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        ReplicatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_TerminalShiftSyncSummaries", x => x.Id);
                    });

                migrationBuilder.CreateIndex(
                    name: "IX_CashierAuditAlerts_AnomalyType",
                    schema: "Restaurant",
                    table: "CashierAuditAlerts",
                    column: "AnomalyType");

                migrationBuilder.CreateIndex(
                    name: "IX_CashierAuditAlerts_CashierName",
                    schema: "Restaurant",
                    table: "CashierAuditAlerts",
                    column: "CashierName");

                migrationBuilder.CreateIndex(
                    name: "IX_CashierAuditAlerts_DetectedAtUtc",
                    schema: "Restaurant",
                    table: "CashierAuditAlerts",
                    column: "DetectedAtUtc");

                migrationBuilder.CreateIndex(
                    name: "IX_CashierAuditAlerts_Status",
                    schema: "Restaurant",
                    table: "CashierAuditAlerts",
                    column: "Status");

                migrationBuilder.CreateIndex(
                    name: "IX_QuickBooksSyncMaps_CreatedAtUtc",
                    schema: "Restaurant",
                    table: "QuickBooksSyncMaps",
                    column: "CreatedAtUtc");

                migrationBuilder.CreateIndex(
                    name: "IX_QuickBooksSyncMaps_EntityType_LocalEntityId",
                    schema: "Restaurant",
                    table: "QuickBooksSyncMaps",
                    columns: new[] { "EntityType", "LocalEntityId" },
                    unique: true);

                migrationBuilder.CreateIndex(
                    name: "IX_QuickBooksSyncMaps_QuickBooksTxnId",
                    schema: "Restaurant",
                    table: "QuickBooksSyncMaps",
                    column: "QuickBooksTxnId");

                migrationBuilder.CreateIndex(
                    name: "IX_QuickBooksSyncMaps_Status",
                    schema: "Restaurant",
                    table: "QuickBooksSyncMaps",
                    column: "Status");

                migrationBuilder.CreateIndex(
                    name: "IX_SyncConflicts_EntityKind_EntityId",
                    schema: "Restaurant",
                    table: "SyncConflicts",
                    columns: new[] { "EntityKind", "EntityId" });

                migrationBuilder.CreateIndex(
                    name: "IX_SyncConflicts_Status",
                    schema: "Restaurant",
                    table: "SyncConflicts",
                    column: "Status");

                migrationBuilder.CreateIndex(
                    name: "IX_SyncInboxRecords_EntityKind_EntityId",
                    schema: "Restaurant",
                    table: "SyncInboxRecords",
                    columns: new[] { "EntityKind", "EntityId" });

                migrationBuilder.CreateIndex(
                    name: "IX_SyncInboxRecords_PacketId",
                    schema: "Restaurant",
                    table: "SyncInboxRecords",
                    column: "PacketId");

                migrationBuilder.CreateIndex(
                    name: "IX_SyncInboxRecords_Status",
                    schema: "Restaurant",
                    table: "SyncInboxRecords",
                    column: "Status");

                migrationBuilder.CreateIndex(
                    name: "IX_TerminalShiftSyncSummaries_Branch_Terminal_Shift",
                    schema: "Restaurant",
                    table: "TerminalShiftSyncSummaries",
                    columns: new[] { "BranchId", "TerminalId", "ShiftNumber" },
                    unique: true);

                migrationBuilder.CreateIndex(
                    name: "IX_Tables_DiningAreaId_Code",
                    schema: "Restaurant",
                    table: "Tables",
                    columns: new[] { "DiningAreaId", "Code" },
                    unique: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("""
                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[QuickBooksSyncMaps]'))
                        DROP TABLE [Restaurant].[QuickBooksSyncMaps];

                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[SyncInboxRecords]'))
                        DROP TABLE [Restaurant].[SyncInboxRecords];

                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[SyncConflicts]'))
                        DROP TABLE [Restaurant].[SyncConflicts];

                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[TerminalShiftSyncSummaries]'))
                        DROP TABLE [Restaurant].[TerminalShiftSyncSummaries];

                    IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[CashierAuditAlerts]'))
                        DROP TABLE [Restaurant].[CashierAuditAlerts];
                    """);
            }
            else
            {
                migrationBuilder.DropTable(
                    name: "CashierAuditAlerts",
                    schema: "Restaurant");

                migrationBuilder.DropTable(
                    name: "QuickBooksSyncMaps",
                    schema: "Restaurant");

                migrationBuilder.DropTable(
                    name: "SyncConflicts",
                    schema: "Restaurant");

                migrationBuilder.DropTable(
                    name: "SyncInboxRecords",
                    schema: "Restaurant");

                migrationBuilder.DropTable(
                    name: "TerminalShiftSyncSummaries",
                    schema: "Restaurant");
            }
        }
    }
}
