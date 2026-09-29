using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clovent.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderRiderPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shifts_CashierId' AND object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
                    DROP INDEX [IX_Shifts_CashierId] ON [Restaurant].[Shifts];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shifts_TerminalId' AND object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
                    DROP INDEX [IX_Shifts_TerminalId] ON [Restaurant].[Shifts];

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Orders]') AND name = 'DeliveryAddress')
                    ALTER TABLE [Restaurant].[Orders] ADD [DeliveryAddress] nvarchar(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Orders]') AND name = 'DeliveryCustomerName')
                    ALTER TABLE [Restaurant].[Orders] ADD [DeliveryCustomerName] nvarchar(150) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Orders]') AND name = 'DeliveryFee')
                    ALTER TABLE [Restaurant].[Orders] ADD [DeliveryFee] decimal(18,2) NOT NULL CONSTRAINT [DF_Orders_DeliveryFee] DEFAULT 0.00;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Orders]') AND name = 'DeliveryNotes')
                    ALTER TABLE [Restaurant].[Orders] ADD [DeliveryNotes] nvarchar(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Orders]') AND name = 'DeliveryPhone')
                    ALTER TABLE [Restaurant].[Orders] ADD [DeliveryPhone] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Orders]') AND name = 'DeliveryStatus')
                    ALTER TABLE [Restaurant].[Orders] ADD [DeliveryStatus] nvarchar(30) NOT NULL CONSTRAINT [DF_Orders_DeliveryStatus] DEFAULT 'None';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Orders]') AND name = 'OrderSource')
                    ALTER TABLE [Restaurant].[Orders] ADD [OrderSource] nvarchar(30) NOT NULL CONSTRAINT [DF_Orders_OrderSource] DEFAULT 'WalkIn';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Orders]') AND name = 'RiderName')
                    ALTER TABLE [Restaurant].[Orders] ADD [RiderName] nvarchar(150) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Orders]') AND name = 'RiderPhone')
                    ALTER TABLE [Restaurant].[Orders] ADD [RiderPhone] nvarchar(50) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[Customers]') AND name = 'IsCreditAllowed')
                    ALTER TABLE [Restaurant].[Customers] ADD [IsCreditAllowed] bit NOT NULL CONSTRAINT [DF_Customers_IsCreditAllowed] DEFAULT 1;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[CustomerLedgerEntries]') AND name = 'PaymentMethod')
                    ALTER TABLE [Restaurant].[CustomerLedgerEntries] ADD [PaymentMethod] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Restaurant].[CustomerLedgerEntries]') AND name = 'ShiftId')
                    ALTER TABLE [Restaurant].[CustomerLedgerEntries] ADD [ShiftId] uniqueidentifier NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[AttendanceSessions]'))
                BEGIN
                    CREATE TABLE [Restaurant].[AttendanceSessions] (
                        [Id] uniqueidentifier NOT NULL,
                        [UserId] uniqueidentifier NOT NULL,
                        [UserName] nvarchar(150) NOT NULL,
                        [BranchId] uniqueidentifier NOT NULL,
                        [BranchName] nvarchar(150) NULL,
                        [PunchInTerminalId] uniqueidentifier NULL,
                        [PunchOutTerminalId] uniqueidentifier NULL,
                        [PunchInAtUtc] datetimeoffset NOT NULL,
                        [PunchOutAtUtc] datetimeoffset NULL,
                        [Status] nvarchar(20) NOT NULL,
                        [Notes] nvarchar(1000) NULL,
                        [CreatedAtUtc] datetimeoffset NOT NULL,
                        [UpdatedAtUtc] datetimeoffset NOT NULL,
                        CONSTRAINT [PK_AttendanceSessions] PRIMARY KEY ([Id])
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[BusinessDayCloses]'))
                BEGIN
                    CREATE TABLE [Restaurant].[BusinessDayCloses] (
                        [Id] uniqueidentifier NOT NULL,
                        [BranchId] uniqueidentifier NOT NULL,
                        [BusinessDate] date NOT NULL,
                        [ClosedAtUtc] datetimeoffset NOT NULL,
                        [ClosedByUserId] uniqueidentifier NOT NULL,
                        [ClosedByUserName] nvarchar(150) NOT NULL,
                        [Status] nvarchar(20) NOT NULL,
                        [TotalSales] decimal(18,2) NOT NULL,
                        [CashSales] decimal(18,2) NOT NULL,
                        [CardSales] decimal(18,2) NOT NULL,
                        [OtherPayments] decimal(18,2) NOT NULL,
                        [Refunds] decimal(18,2) NOT NULL,
                        [Discounts] decimal(18,2) NOT NULL,
                        [Tax] decimal(18,2) NOT NULL,
                        [CashIn] decimal(18,2) NOT NULL,
                        [CashOut] decimal(18,2) NOT NULL,
                        [ShiftCount] int NOT NULL,
                        [TotalShiftVariance] decimal(18,2) NOT NULL,
                        [OrderCount] int NOT NULL,
                        [Notes] nvarchar(1000) NULL,
                        [CreatedAtUtc] datetimeoffset NOT NULL,
                        CONSTRAINT [PK_BusinessDayCloses] PRIMARY KEY ([Id])
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[CustomerPaymentAllocations]'))
                BEGIN
                    CREATE TABLE [Restaurant].[CustomerPaymentAllocations] (
                        [Id] uniqueidentifier NOT NULL,
                        [CustomerId] uniqueidentifier NOT NULL,
                        [OrderId] uniqueidentifier NOT NULL,
                        [CustomerLedgerEntryId] uniqueidentifier NULL,
                        [Amount] decimal(18,2) NOT NULL,
                        [AllocatedAtUtc] datetimeoffset NOT NULL,
                        [Notes] nvarchar(500) NULL,
                        CONSTRAINT [PK_CustomerPaymentAllocations] PRIMARY KEY ([Id])
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shifts_CashierId_Active' AND object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
                    CREATE UNIQUE INDEX [IX_Shifts_CashierId_Active] ON [Restaurant].[Shifts] ([CashierId]) WHERE [Status] = 'Open';
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shifts_TerminalId_Active' AND object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
                    CREATE UNIQUE INDEX [IX_Shifts_TerminalId_Active] ON [Restaurant].[Shifts] ([TerminalId]) WHERE [Status] = 'Open';
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerLedgerEntries_ShiftId' AND object_id = OBJECT_ID(N'[Restaurant].[CustomerLedgerEntries]'))
                    CREATE INDEX [IX_CustomerLedgerEntries_ShiftId] ON [Restaurant].[CustomerLedgerEntries] ([ShiftId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceSessions_BranchId' AND object_id = OBJECT_ID(N'[Restaurant].[AttendanceSessions]'))
                    CREATE INDEX [IX_AttendanceSessions_BranchId] ON [Restaurant].[AttendanceSessions] ([BranchId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceSessions_PunchInAtUtc' AND object_id = OBJECT_ID(N'[Restaurant].[AttendanceSessions]'))
                    CREATE INDEX [IX_AttendanceSessions_PunchInAtUtc] ON [Restaurant].[AttendanceSessions] ([PunchInAtUtc]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceSessions_Status' AND object_id = OBJECT_ID(N'[Restaurant].[AttendanceSessions]'))
                    CREATE INDEX [IX_AttendanceSessions_Status] ON [Restaurant].[AttendanceSessions] ([Status]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceSessions_UserId_Open' AND object_id = OBJECT_ID(N'[Restaurant].[AttendanceSessions]'))
                    CREATE UNIQUE INDEX [IX_AttendanceSessions_UserId_Open] ON [Restaurant].[AttendanceSessions] ([UserId]) WHERE [PunchOutAtUtc] IS NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BusinessDayCloses_BranchId_BusinessDate' AND object_id = OBJECT_ID(N'[Restaurant].[BusinessDayCloses]'))
                    CREATE UNIQUE INDEX [IX_BusinessDayCloses_BranchId_BusinessDate] ON [Restaurant].[BusinessDayCloses] ([BranchId], [BusinessDate]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerPaymentAllocations_CustomerId' AND object_id = OBJECT_ID(N'[Restaurant].[CustomerPaymentAllocations]'))
                    CREATE INDEX [IX_CustomerPaymentAllocations_CustomerId] ON [Restaurant].[CustomerPaymentAllocations] ([CustomerId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerPaymentAllocations_CustomerLedgerEntryId' AND object_id = OBJECT_ID(N'[Restaurant].[CustomerPaymentAllocations]'))
                    CREATE INDEX [IX_CustomerPaymentAllocations_CustomerLedgerEntryId] ON [Restaurant].[CustomerPaymentAllocations] ([CustomerLedgerEntryId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerPaymentAllocations_OrderId' AND object_id = OBJECT_ID(N'[Restaurant].[CustomerPaymentAllocations]'))
                    CREATE INDEX [IX_CustomerPaymentAllocations_OrderId] ON [Restaurant].[CustomerPaymentAllocations] ([OrderId]);
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceSessions",
                schema: "Restaurant");

            migrationBuilder.DropTable(
                name: "BusinessDayCloses",
                schema: "Restaurant");

            migrationBuilder.DropTable(
                name: "CustomerPaymentAllocations",
                schema: "Restaurant");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_CashierId_Active",
                schema: "Restaurant",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_TerminalId_Active",
                schema: "Restaurant",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerLedgerEntries_ShiftId",
                schema: "Restaurant",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                schema: "Restaurant",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryCustomerName",
                schema: "Restaurant",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                schema: "Restaurant",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryNotes",
                schema: "Restaurant",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryPhone",
                schema: "Restaurant",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                schema: "Restaurant",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderSource",
                schema: "Restaurant",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RiderName",
                schema: "Restaurant",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RiderPhone",
                schema: "Restaurant",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsCreditAllowed",
                schema: "Restaurant",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                schema: "Restaurant",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                schema: "Restaurant",
                table: "CustomerLedgerEntries");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_CashierId",
                schema: "Restaurant",
                table: "Shifts",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_TerminalId",
                schema: "Restaurant",
                table: "Shifts",
                column: "TerminalId");
        }
    }
}
