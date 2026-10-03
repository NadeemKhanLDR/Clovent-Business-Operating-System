
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'Clovent_BusinessOperatingSystem')
BEGIN
    CREATE DATABASE [Clovent_BusinessOperatingSystem]
    COLLATE SQL_Latin1_General_CP1_CI_AS;
    
    ALTER DATABASE [Clovent_BusinessOperatingSystem] SET RECOVERY SIMPLE;
    ALTER DATABASE [Clovent_BusinessOperatingSystem] MODIFY FILE (NAME = N'Clovent_BusinessOperatingSystem', SIZE = 128MB, FILEGROWTH = 64MB);
    ALTER DATABASE [Clovent_BusinessOperatingSystem] MODIFY FILE (NAME = N'Clovent_BusinessOperatingSystem_log', SIZE = 64MB, FILEGROWTH = 32MB);
END
GO

USE [Clovent_BusinessOperatingSystem];
GO


IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Authentication')
BEGIN
    EXEC('CREATE SCHEMA [Authentication]');
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Identity')
BEGIN
    EXEC('CREATE SCHEMA [Identity]');
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'MasterData')
BEGIN
    EXEC('CREATE SCHEMA [MasterData]');
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Catalog')
BEGIN
    EXEC('CREATE SCHEMA [Catalog]');
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Inventory')
BEGIN
    EXEC('CREATE SCHEMA [Inventory]');
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Restaurant')
BEGIN
    EXEC('CREATE SCHEMA [Restaurant]');
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Authentication' AND t.name = 'LoginAttempts')
BEGIN
CREATE TABLE [Authentication].[LoginAttempts] (
    [Id] [uniqueidentifier] NOT NULL,
    [AttemptedIdentifier] [nvarchar](320) NOT NULL,
    [UserId] [uniqueidentifier] NULL,
    [Outcome] [nvarchar](30) NOT NULL,
    [IpAddress] [nvarchar](45) NULL,
    [OccurredAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_LoginAttempts] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Authentication' AND t.name = 'RefreshSessions')
BEGIN
CREATE TABLE [Authentication].[RefreshSessions] (
    [Id] [uniqueidentifier] NOT NULL,
    [SessionId] [uniqueidentifier] NOT NULL,
    [IssuedAtUtc] [datetimeoffset] NOT NULL,
    [ExpiresAtUtc] [datetimeoffset] NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    CONSTRAINT [PK_RefreshSessions] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Authentication' AND t.name = 'Sessions')
BEGIN
CREATE TABLE [Authentication].[Sessions] (
    [Id] [uniqueidentifier] NOT NULL,
    [UserId] [uniqueidentifier] NOT NULL,
    [IpAddress] [nvarchar](45) NULL,
    [IdleTimeout] [bigint] NOT NULL,
    [StartedAtUtc] [datetimeoffset] NOT NULL,
    [LastActivityAtUtc] [datetimeoffset] NOT NULL,
    [ExpiresAtUtc] [datetimeoffset] NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    CONSTRAINT [PK_Sessions] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Authentication' AND t.name = 'UserCredentials')
BEGIN
CREATE TABLE [Authentication].[UserCredentials] (
    [Id] [uniqueidentifier] NOT NULL,
    [UserId] [uniqueidentifier] NOT NULL,
    [PasswordHash] [nvarchar](512) NULL,
    [PinHash] [nvarchar](512) NULL,
    [SecurityStamp] [nvarchar](64) NOT NULL,
    [PasswordHistory] [nvarchar](max) NOT NULL,
    [FailedAttempts] [int] NOT NULL,
    CONSTRAINT [PK_UserCredentials] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Identity' AND t.name = 'Branches')
BEGIN
CREATE TABLE [Identity].[Branches] (
    [Id] [uniqueidentifier] NOT NULL,
    [CompanyId] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](200) NOT NULL,
    [Address] [nvarchar](max) NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Branches] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Identity' AND t.name = 'Companies')
BEGIN
CREATE TABLE [Identity].[Companies] (
    [Id] [uniqueidentifier] NOT NULL,
    [OrganizationId] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](200) NOT NULL,
    [TaxId] [nvarchar](50) NULL,
    [Status] [nvarchar](20) NOT NULL,
    [BranchIds] [nvarchar](max) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Identity' AND t.name = 'Organizations')
BEGIN
CREATE TABLE [Identity].[Organizations] (
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](200) NOT NULL,
    [TaxId] [nvarchar](50) NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CompanyIds] [nvarchar](max) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Identity' AND t.name = 'Permissions')
BEGIN
CREATE TABLE [Identity].[Permissions] (
    [Id] [uniqueidentifier] NOT NULL,
    [Code] [nvarchar](260) NOT NULL,
    [Description] [nvarchar](500) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Identity' AND t.name = 'Roles')
BEGIN
CREATE TABLE [Identity].[Roles] (
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](64) NOT NULL,
    [PermissionIds] [nvarchar](max) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Identity' AND t.name = 'Users')
BEGIN
CREATE TABLE [Identity].[Users] (
    [Id] [uniqueidentifier] NOT NULL,
    [Email] [nvarchar](254) NOT NULL,
    [UserName] [nvarchar](32) NOT NULL,
    [DisplayName] [nvarchar](100) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [RoleIds] [nvarchar](max) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [BranchId] [uniqueidentifier] NULL,
    [CompanyId] [uniqueidentifier] NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = 'BusinessSettings')
BEGIN
CREATE TABLE [MasterData].[BusinessSettings] (
    [Id] [uniqueidentifier] NOT NULL,
    [OrganizationId] [uniqueidentifier] NOT NULL,
    [DefaultCurrencyId] [uniqueidentifier] NOT NULL,
    [DefaultLanguageId] [uniqueidentifier] NOT NULL,
    [DefaultTimeZoneId] [uniqueidentifier] NOT NULL,
    [DefaultFiscalYearId] [uniqueidentifier] NULL,
    [DateFormat] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [UpdatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_BusinessSettings] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = 'Currencies')
BEGIN
CREATE TABLE [MasterData].[Currencies] (
    [Id] [uniqueidentifier] NOT NULL,
    [Code] [nvarchar](3) NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [Symbol] [nvarchar](10) NOT NULL,
    [DecimalPlaces] [int] NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Currencies] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = 'Departments')
BEGIN
CREATE TABLE [MasterData].[Departments] (
    [Id] [uniqueidentifier] NOT NULL,
    [BranchId] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](200) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = 'FiscalYears')
BEGIN
CREATE TABLE [MasterData].[FiscalYears] (
    [Id] [uniqueidentifier] NOT NULL,
    [OrganizationId] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](200) NOT NULL,
    [StartDate] [date] NOT NULL,
    [EndDate] [date] NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_FiscalYears] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = 'Languages')
BEGIN
CREATE TABLE [MasterData].[Languages] (
    [Id] [uniqueidentifier] NOT NULL,
    [Code] [nvarchar](2) NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [NativeName] [nvarchar](100) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Languages] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = 'Terminals')
BEGIN
CREATE TABLE [MasterData].[Terminals] (
    [Id] [uniqueidentifier] NOT NULL,
    [BranchId] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](200) NOT NULL,
    [Code] [nvarchar](20) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Terminals] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = 'TimeZoneEntries')
BEGIN
CREATE TABLE [MasterData].[TimeZoneEntries] (
    [Id] [uniqueidentifier] NOT NULL,
    [IanaId] [nvarchar](100) NOT NULL,
    [DisplayName] [nvarchar](100) NOT NULL,
    [UtcOffsetMinutes] [int] NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_TimeZoneEntries] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = 'Warehouses')
BEGIN
CREATE TABLE [MasterData].[Warehouses] (
    [Id] [uniqueidentifier] NOT NULL,
    [BranchId] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](200) NOT NULL,
    [Code] [nvarchar](20) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Catalog' AND t.name = 'Barcodes')
BEGIN
CREATE TABLE [Catalog].[Barcodes] (
    [Id] [uniqueidentifier] NOT NULL,
    [ProductVariantId] [uniqueidentifier] NOT NULL,
    [Value] [nvarchar](14) NOT NULL,
    [IsPrimary] [bit] NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Barcodes] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Catalog' AND t.name = 'Brands')
BEGIN
CREATE TABLE [Catalog].[Brands] (
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Brands] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Catalog' AND t.name = 'ProductCategories')
BEGIN
CREATE TABLE [Catalog].[ProductCategories] (
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [ParentCategoryId] [uniqueidentifier] NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [ColorHex] [nvarchar](7) NULL,
    [SortOrder] [int] CONSTRAINT [DF__ProductCa__SortO__6EF57B66] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Catalog' AND t.name = 'ProductGroups')
BEGIN
CREATE TABLE [Catalog].[ProductGroups] (
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_ProductGroups] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Catalog' AND t.name = 'ProductPrices')
BEGIN
CREATE TABLE [Catalog].[ProductPrices] (
    [Id] [uniqueidentifier] NOT NULL,
    [ProductVariantId] [uniqueidentifier] NOT NULL,
    [PriceType] [nvarchar](20) NOT NULL,
    [Amount] [decimal](18,2) NOT NULL,
    [CurrencyId] [uniqueidentifier] NOT NULL,
    [EffectiveFromUtc] [datetimeoffset] NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_ProductPrices] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Catalog' AND t.name = 'Products')
BEGIN
CREATE TABLE [Catalog].[Products] (
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](200) NOT NULL,
    [Sku] [nvarchar](40) NOT NULL,
    [CategoryId] [uniqueidentifier] NULL,
    [GroupId] [uniqueidentifier] NULL,
    [BrandId] [uniqueidentifier] NULL,
    [BaseUnitOfMeasureId] [uniqueidentifier] NOT NULL,
    [TaxConfiguration] [nvarchar](max) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [ItemType] [nvarchar](30) CONSTRAINT [DF_Products_ItemType] DEFAULT (N'Prepared') NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Catalog' AND t.name = 'ProductVariants')
BEGIN
CREATE TABLE [Catalog].[ProductVariants] (
    [Id] [uniqueidentifier] NOT NULL,
    [ProductId] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](200) NOT NULL,
    [Sku] [nvarchar](40) NOT NULL,
    [UnitOfMeasureId] [uniqueidentifier] NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [SortOrder] [int] CONSTRAINT [DF__ProductVa__SortO__6E01572D] DEFAULT ((0)) NOT NULL,
    [IsAvailable] [bit] CONSTRAINT [DF_ProductVariants_IsAvailable] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_ProductVariants] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Catalog' AND t.name = 'UnitsOfMeasure')
BEGIN
CREATE TABLE [Catalog].[UnitsOfMeasure] (
    [Id] [uniqueidentifier] NOT NULL,
    [Code] [nvarchar](10) NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_UnitsOfMeasure] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Inventory' AND t.name = 'InventoryTransactions')
BEGIN
CREATE TABLE [Inventory].[InventoryTransactions] (
    [Id] [uniqueidentifier] NOT NULL,
    [WarehouseId] [uniqueidentifier] NOT NULL,
    [ProductVariantId] [uniqueidentifier] NOT NULL,
    [TransactionType] [nvarchar](20) NOT NULL,
    [Quantity] [decimal](18,4) NOT NULL,
    [ReferenceType] [nvarchar](100) NULL,
    [ReferenceId] [uniqueidentifier] NULL,
    [Notes] [nvarchar](500) NULL,
    [OccurredAtUtc] [bigint] NOT NULL,
    CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Inventory' AND t.name = 'StockAdjustments')
BEGIN
CREATE TABLE [Inventory].[StockAdjustments] (
    [Id] [uniqueidentifier] NOT NULL,
    [WarehouseId] [uniqueidentifier] NOT NULL,
    [ProductVariantId] [uniqueidentifier] NOT NULL,
    [AdjustmentType] [nvarchar](20) NOT NULL,
    [Quantity] [decimal](18,4) NOT NULL,
    [Reason] [nvarchar](500) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [AppliedAtUtc] [datetimeoffset] NULL,
    CONSTRAINT [PK_StockAdjustments] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Inventory' AND t.name = 'StockTransfers')
BEGIN
CREATE TABLE [Inventory].[StockTransfers] (
    [Id] [uniqueidentifier] NOT NULL,
    [SourceWarehouseId] [uniqueidentifier] NOT NULL,
    [DestinationWarehouseId] [uniqueidentifier] NOT NULL,
    [ProductVariantId] [uniqueidentifier] NOT NULL,
    [Quantity] [decimal](18,4) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [CompletedAtUtc] [datetimeoffset] NULL,
    CONSTRAINT [PK_StockTransfers] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Inventory' AND t.name = 'WarehouseStocks')
BEGIN
CREATE TABLE [Inventory].[WarehouseStocks] (
    [Id] [uniqueidentifier] NOT NULL,
    [WarehouseId] [uniqueidentifier] NOT NULL,
    [ProductVariantId] [uniqueidentifier] NOT NULL,
    [QuantityOnHand] [decimal](18,4) NOT NULL,
    [QuantityReserved] [decimal](18,4) NOT NULL,
    [MinimumStock] [decimal](18,4) NOT NULL,
    [MaximumStock] [decimal](18,4) NOT NULL,
    [AllowNegativeStock] [bit] NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [UpdatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_WarehouseStocks] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'ActivityLogEntries')
BEGIN
CREATE TABLE [Restaurant].[ActivityLogEntries] (
    [Id] [uniqueidentifier] NOT NULL,
    [Action] [nvarchar](100) NOT NULL,
    [Details] [nvarchar](1000) NULL,
    [PerformedBy] [nvarchar](200) NOT NULL,
    [MachineName] [nvarchar](100) NOT NULL,
    [OccurredAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_ActivityLogEntries] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'AttendanceSessions')
BEGIN
CREATE TABLE [Restaurant].[AttendanceSessions] (
    [Id] [uniqueidentifier] NOT NULL,
    [UserId] [uniqueidentifier] NOT NULL,
    [UserName] [nvarchar](150) NOT NULL,
    [BranchId] [uniqueidentifier] NOT NULL,
    [BranchName] [nvarchar](150) NULL,
    [PunchInTerminalId] [uniqueidentifier] NULL,
    [PunchOutTerminalId] [uniqueidentifier] NULL,
    [PunchInAtUtc] [datetimeoffset] NOT NULL,
    [PunchOutAtUtc] [datetimeoffset] NULL,
    [Status] [nvarchar](20) NOT NULL,
    [Notes] [nvarchar](1000) NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [UpdatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_AttendanceSessions] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'BusinessDayCloses')
BEGIN
CREATE TABLE [Restaurant].[BusinessDayCloses] (
    [Id] [uniqueidentifier] NOT NULL,
    [BranchId] [uniqueidentifier] NOT NULL,
    [BusinessDate] [date] NOT NULL,
    [ClosedAtUtc] [datetimeoffset] NOT NULL,
    [ClosedByUserId] [uniqueidentifier] NOT NULL,
    [ClosedByUserName] [nvarchar](150) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [TotalSales] [decimal](18,2) NOT NULL,
    [CashSales] [decimal](18,2) NOT NULL,
    [CardSales] [decimal](18,2) NOT NULL,
    [OtherPayments] [decimal](18,2) NOT NULL,
    [Refunds] [decimal](18,2) NOT NULL,
    [Discounts] [decimal](18,2) NOT NULL,
    [Tax] [decimal](18,2) NOT NULL,
    [CashIn] [decimal](18,2) NOT NULL,
    [CashOut] [decimal](18,2) NOT NULL,
    [ShiftCount] [int] NOT NULL,
    [TotalShiftVariance] [decimal](18,2) NOT NULL,
    [OrderCount] [int] NOT NULL,
    [Notes] [nvarchar](1000) NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_BusinessDayCloses] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'CashMovements')
BEGIN
CREATE TABLE [Restaurant].[CashMovements] (
    [Id] [uniqueidentifier] NOT NULL,
    [ShiftId] [uniqueidentifier] NOT NULL,
    [Type] [nvarchar](20) NOT NULL,
    [Amount] [decimal](18,2) NOT NULL,
    [Reason] [nvarchar](250) NOT NULL,
    [UserId] [uniqueidentifier] NOT NULL,
    [TimestampUtc] [datetimeoffset] NOT NULL,
    [Notes] [nvarchar](500) NULL,
    CONSTRAINT [PK_CashMovements] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'ComboDecisions')
BEGIN
CREATE TABLE [Restaurant].[ComboDecisions] (
    [WarehouseId] [uniqueidentifier] NOT NULL,
    [Signature] [nvarchar](100) NOT NULL,
    [TemplateId] [uniqueidentifier] NULL,
    [UserId] [uniqueidentifier] NOT NULL,
    [DecidedAtUtc] [datetimeoffset] NOT NULL,
    [DismissedUntilUtc] [datetimeoffset] NULL,
    [Reason] [nvarchar](250) NULL,
    [Version] [uniqueidentifier] NOT NULL,
    CONSTRAINT [PK_ComboDecisions] PRIMARY KEY ([WarehouseId] ASC, [Signature] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'CustomerLedgerEntries')
BEGIN
CREATE TABLE [Restaurant].[CustomerLedgerEntries] (
    [Id] [uniqueidentifier] NOT NULL,
    [CustomerId] [uniqueidentifier] NOT NULL,
    [Date] [datetimeoffset] NOT NULL,
    [Reference] [nvarchar](100) NOT NULL,
    [Description] [nvarchar](500) NOT NULL,
    [Debit] [decimal](18,2) NOT NULL,
    [Credit] [decimal](18,2) NOT NULL,
    [RunningBalance] [decimal](18,2) NOT NULL,
    [ShiftId] [uniqueidentifier] NULL,
    [PaymentMethod] [nvarchar](50) NULL,
    CONSTRAINT [PK_CustomerLedgerEntries] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'CustomerPaymentAllocations')
BEGIN
CREATE TABLE [Restaurant].[CustomerPaymentAllocations] (
    [Id] [uniqueidentifier] NOT NULL,
    [CustomerId] [uniqueidentifier] NOT NULL,
    [OrderId] [uniqueidentifier] NOT NULL,
    [CustomerLedgerEntryId] [uniqueidentifier] NULL,
    [Amount] [decimal](18,2) NOT NULL,
    [AllocatedAtUtc] [datetimeoffset] NOT NULL,
    [Notes] [nvarchar](500) NULL,
    CONSTRAINT [PK_CustomerPaymentAllocations] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'Customers')
BEGIN
CREATE TABLE [Restaurant].[Customers] (
    [Id] [uniqueidentifier] NOT NULL,
    [Code] [nvarchar](20) NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [MobileNumber] [nvarchar](20) NOT NULL,
    [Address] [nvarchar](500) NOT NULL,
    [Email] [nvarchar](100) NULL,
    [OpeningBalance] [decimal](18,2) NOT NULL,
    [CreditLimit] [decimal](18,2) NOT NULL,
    [OutstandingBalance] [decimal](18,2) NOT NULL,
    [IsActive] [bit] NOT NULL,
    [Notes] [nvarchar](1000) NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [UpdatedAtUtc] [datetimeoffset] NOT NULL,
    [Mobile2] [nvarchar](20) NULL,
    [Phone] [nvarchar](20) NULL,
    [ShopNo] [nvarchar](100) NULL,
    [IsDefault] [bit] CONSTRAINT [DF__Customers__IsDef__123F82FA] DEFAULT (CONVERT([bit],(0))) NOT NULL,
    [IsCreditAllowed] [bit] CONSTRAINT [DF_Customers_IsCreditAllowed] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'DailySalesSequences')
BEGIN
CREATE TABLE [Restaurant].[DailySalesSequences] (
    [Id] [uniqueidentifier] NOT NULL,
    [WarehouseId] [uniqueidentifier] NOT NULL,
    [Date] [date] NOT NULL,
    [LastNumber] [int] NOT NULL,
    CONSTRAINT [PK_DailySalesSequences] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'DiningAreas')
BEGIN
CREATE TABLE [Restaurant].[DiningAreas] (
    [Id] [uniqueidentifier] NOT NULL,
    [BranchId] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_DiningAreas] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'Discounts')
BEGIN
CREATE TABLE [Restaurant].[Discounts] (
    [Id] [uniqueidentifier] NOT NULL,
    [OrderId] [uniqueidentifier] NOT NULL,
    [DiscountType] [nvarchar](20) NOT NULL,
    [Value] [decimal](18,4) NOT NULL,
    [Reason] [nvarchar](500) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Discounts] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'KitchenTickets')
BEGIN
CREATE TABLE [Restaurant].[KitchenTickets] (
    [Id] [uniqueidentifier] NOT NULL,
    [OrderId] [uniqueidentifier] NOT NULL,
    [OrderLineIds] [nvarchar](max) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [StartedAtUtc] [datetimeoffset] NULL,
    [ReadyAtUtc] [datetimeoffset] NULL,
    [ServedAtUtc] [datetimeoffset] NULL,
    CONSTRAINT [PK_KitchenTickets] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'OrderLines')
BEGIN
CREATE TABLE [Restaurant].[OrderLines] (
    [Id] [uniqueidentifier] NOT NULL,
    [OrderId] [uniqueidentifier] NOT NULL,
    [ProductVariantId] [uniqueidentifier] NOT NULL,
    [Quantity] [decimal](18,4) NOT NULL,
    [UnitPrice] [decimal](18,2) NOT NULL,
    [TaxRatePercentage] [decimal](5,2) NOT NULL,
    [TaxIsInclusive] [bit] NOT NULL,
    [Notes] [nvarchar](500) NULL,
    [IsVoided] [bit] NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [IsPriceOverridden] [bit] CONSTRAINT [DF__OrderLine__IsPri__02FC7413] DEFAULT (CONVERT([bit],(0))) NOT NULL,
    [OriginalUnitPrice] [decimal](18,2) NOT NULL,
    [PriceOverriddenAtUtc] [datetimeoffset] NULL,
    [PriceOverriddenBy] [nvarchar](200) NULL,
    [PriceOverrideReason] [nvarchar](500) NULL,
    CONSTRAINT [PK_OrderLines] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'OrderNumberSequences')
BEGIN
CREATE TABLE [Restaurant].[OrderNumberSequences] (
    [Id] [uniqueidentifier] NOT NULL,
    [Prefix] [nvarchar](20) NOT NULL,
    [NextNumber] [int] NOT NULL,
    CONSTRAINT [PK_OrderNumberSequences] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'Orders')
BEGIN
CREATE TABLE [Restaurant].[Orders] (
    [Id] [uniqueidentifier] NOT NULL,
    [OrderNumber] [nvarchar](40) NOT NULL,
    [OrderType] [nvarchar](20) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [TableId] [uniqueidentifier] NULL,
    [WarehouseId] [uniqueidentifier] NOT NULL,
    [Notes] [nvarchar](1000) NULL,
    [CustomerNotes] [nvarchar](1000) NULL,
    [OrderLineIds] [nvarchar](max) NOT NULL,
    [DiscountIds] [nvarchar](max) NOT NULL,
    [ServiceChargeIds] [nvarchar](max) NOT NULL,
    [PaymentIds] [nvarchar](max) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [UpdatedAtUtc] [datetimeoffset] NOT NULL,
    [DailySalesNumber] [int] NULL,
    [CustomerId] [uniqueidentifier] NULL,
    [OrderSource] [nvarchar](30) CONSTRAINT [DF_Orders_OrderSource] DEFAULT ('WalkIn') NOT NULL,
    [DeliveryStatus] [nvarchar](30) CONSTRAINT [DF_Orders_DeliveryStatus] DEFAULT ('None') NOT NULL,
    [DeliveryCustomerName] [nvarchar](150) NULL,
    [DeliveryPhone] [nvarchar](50) NULL,
    [DeliveryAddress] [nvarchar](500) NULL,
    [DeliveryNotes] [nvarchar](500) NULL,
    [DeliveryFee] [decimal](18,2) CONSTRAINT [DF_Orders_DeliveryFee] DEFAULT ((0.00)) NOT NULL,
    [RiderName] [nvarchar](150) NULL,
    [RiderPhone] [nvarchar](50) NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'PaymentMethods')
BEGIN
CREATE TABLE [Restaurant].[PaymentMethods] (
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](50) NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_PaymentMethods] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'Payments')
BEGIN
CREATE TABLE [Restaurant].[Payments] (
    [Id] [uniqueidentifier] NOT NULL,
    [OrderId] [uniqueidentifier] NOT NULL,
    [PaymentMethodId] [uniqueidentifier] NOT NULL,
    [Amount] [decimal](18,2) NOT NULL,
    [IsVoided] [bit] NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [ShiftId] [uniqueidentifier] NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'QuickOrderTemplateItems')
BEGIN
CREATE TABLE [Restaurant].[QuickOrderTemplateItems] (
    [Id] [uniqueidentifier] NOT NULL,
    [TemplateId] [uniqueidentifier] NOT NULL,
    [VariantId] [uniqueidentifier] NOT NULL,
    [Quantity] [decimal](18,3) NOT NULL,
    [TemplateUnitPrice] [decimal](18,4) NULL,
    CONSTRAINT [PK_QuickOrderTemplateItems] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'QuickOrderTemplates')
BEGIN
CREATE TABLE [Restaurant].[QuickOrderTemplates] (
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [Description] [nvarchar](500) NULL,
    [IsActive] [bit] NOT NULL,
    [DisplayOrder] [int] NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [UpdatedAtUtc] [datetimeoffset] NOT NULL,
    [WarehouseId] [uniqueidentifier] NULL,
    CONSTRAINT [PK_QuickOrderTemplates] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'RecommendationRules')
BEGIN
CREATE TABLE [Restaurant].[RecommendationRules] (
    [Id] [uniqueidentifier] NOT NULL,
    [ProductId] [uniqueidentifier] NULL,
    [RecommendedVariantId] [uniqueidentifier] NOT NULL,
    [Priority] [int] NOT NULL,
    [IsActive] [bit] NOT NULL,
    [StartTime] [time] NULL,
    [EndTime] [time] NULL,
    [DaysOfWeek] [int] NULL,
    [Notes] [nvarchar](500) NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [UpdatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_RecommendationRules] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'ServiceCharges')
BEGIN
CREATE TABLE [Restaurant].[ServiceCharges] (
    [Id] [uniqueidentifier] NOT NULL,
    [OrderId] [uniqueidentifier] NOT NULL,
    [ServiceChargeType] [nvarchar](20) NOT NULL,
    [Value] [decimal](18,4) NOT NULL,
    [Reason] [nvarchar](500) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_ServiceCharges] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'Shifts')
BEGIN
CREATE TABLE [Restaurant].[Shifts] (
    [Id] [uniqueidentifier] NOT NULL,
    [ShiftNumber] [int] NOT NULL,
    [BranchId] [uniqueidentifier] NOT NULL,
    [WarehouseId] [uniqueidentifier] NOT NULL,
    [TerminalId] [uniqueidentifier] NOT NULL,
    [CashierId] [uniqueidentifier] NOT NULL,
    [CashierName] [nvarchar](150) NOT NULL,
    [OpenedAtUtc] [datetimeoffset] NOT NULL,
    [ClosedAtUtc] [datetimeoffset] NULL,
    [Status] [nvarchar](20) NOT NULL,
    [StartingCash] [decimal](18,2) NOT NULL,
    [ExpectedCash] [decimal](18,2) NOT NULL,
    [CountedCash] [decimal](18,2) NOT NULL,
    [CashVariance] [decimal](18,2) NOT NULL,
    [VarianceReason] [nvarchar](500) NULL,
    [Notes] [nvarchar](1000) NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [UpdatedAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_Shifts] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'SuggestionEvents')
BEGIN
CREATE TABLE [Restaurant].[SuggestionEvents] (
    [Id] [uniqueidentifier] NOT NULL,
    [OrderId] [uniqueidentifier] NOT NULL,
    [VariantId] [uniqueidentifier] NOT NULL,
    [TriggerVariantId] [uniqueidentifier] NULL,
    [Kind] [int] NOT NULL,
    [OrderLineId] [uniqueidentifier] NULL,
    [AcceptedQuantity] [decimal](18,2) NOT NULL,
    [AcceptedUnitAmount] [decimal](18,2) NOT NULL,
    [OccurredAtUtc] [datetimeoffset] NOT NULL,
    CONSTRAINT [PK_SuggestionEvents] PRIMARY KEY ([Id] ASC)
);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = 'Tables')
BEGIN
CREATE TABLE [Restaurant].[Tables] (
    [Id] [uniqueidentifier] NOT NULL,
    [DiningAreaId] [uniqueidentifier] NOT NULL,
    [Code] [nvarchar](20) NOT NULL,
    [Capacity] [int] NOT NULL,
    [Status] [nvarchar](20) NOT NULL,
    [OccupancyStatus] [nvarchar](20) NOT NULL,
    [CreatedAtUtc] [datetimeoffset] NOT NULL,
    [Name] [nvarchar](100) CONSTRAINT [DF__Tables__Name__7A67F969] DEFAULT (N'') NOT NULL,
    CONSTRAINT [PK_Tables] PRIMARY KEY ([Id] ASC)
);
END
GO


-- INDEXES


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoginAttempts_AttemptedIdentifier' AND object_id = OBJECT_ID(N'[Authentication].[LoginAttempts]'))
BEGIN
    CREATE INDEX [IX_LoginAttempts_AttemptedIdentifier] ON [Authentication].[LoginAttempts] ([AttemptedIdentifier] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoginAttempts_UserId' AND object_id = OBJECT_ID(N'[Authentication].[LoginAttempts]'))
BEGIN
    CREATE INDEX [IX_LoginAttempts_UserId] ON [Authentication].[LoginAttempts] ([UserId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshSessions_SessionId' AND object_id = OBJECT_ID(N'[Authentication].[RefreshSessions]'))
BEGIN
    CREATE INDEX [IX_RefreshSessions_SessionId] ON [Authentication].[RefreshSessions] ([SessionId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshSessions_SessionId_Status' AND object_id = OBJECT_ID(N'[Authentication].[RefreshSessions]'))
BEGIN
    CREATE INDEX [IX_RefreshSessions_SessionId_Status] ON [Authentication].[RefreshSessions] ([SessionId] ASC, [Status] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sessions_UserId' AND object_id = OBJECT_ID(N'[Authentication].[Sessions]'))
BEGIN
    CREATE INDEX [IX_Sessions_UserId] ON [Authentication].[Sessions] ([UserId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sessions_UserId_Status' AND object_id = OBJECT_ID(N'[Authentication].[Sessions]'))
BEGIN
    CREATE INDEX [IX_Sessions_UserId_Status] ON [Authentication].[Sessions] ([UserId] ASC, [Status] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserCredentials_UserId' AND object_id = OBJECT_ID(N'[Authentication].[UserCredentials]'))
BEGIN
    CREATE UNIQUE INDEX [IX_UserCredentials_UserId] ON [Authentication].[UserCredentials] ([UserId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Branches_CompanyId' AND object_id = OBJECT_ID(N'[Identity].[Branches]'))
BEGIN
    CREATE INDEX [IX_Branches_CompanyId] ON [Identity].[Branches] ([CompanyId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Companies_OrganizationId' AND object_id = OBJECT_ID(N'[Identity].[Companies]'))
BEGIN
    CREATE INDEX [IX_Companies_OrganizationId] ON [Identity].[Companies] ([OrganizationId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Permissions_Code' AND object_id = OBJECT_ID(N'[Identity].[Permissions]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Code] ON [Identity].[Permissions] ([Code] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_Name' AND object_id = OBJECT_ID(N'[Identity].[Roles]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Identity].[Roles] ([Name] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID(N'[Identity].[Users]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Identity].[Users] ([Email] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_UserName' AND object_id = OBJECT_ID(N'[Identity].[Users]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Users_UserName] ON [Identity].[Users] ([UserName] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BusinessSettings_OrganizationId' AND object_id = OBJECT_ID(N'[MasterData].[BusinessSettings]'))
BEGIN
    CREATE UNIQUE INDEX [IX_BusinessSettings_OrganizationId] ON [MasterData].[BusinessSettings] ([OrganizationId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Currencies_Code' AND object_id = OBJECT_ID(N'[MasterData].[Currencies]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Currencies_Code] ON [MasterData].[Currencies] ([Code] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_BranchId' AND object_id = OBJECT_ID(N'[MasterData].[Departments]'))
BEGIN
    CREATE INDEX [IX_Departments_BranchId] ON [MasterData].[Departments] ([BranchId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FiscalYears_OrganizationId' AND object_id = OBJECT_ID(N'[MasterData].[FiscalYears]'))
BEGIN
    CREATE INDEX [IX_FiscalYears_OrganizationId] ON [MasterData].[FiscalYears] ([OrganizationId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Languages_Code' AND object_id = OBJECT_ID(N'[MasterData].[Languages]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Languages_Code] ON [MasterData].[Languages] ([Code] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Terminals_BranchId' AND object_id = OBJECT_ID(N'[MasterData].[Terminals]'))
BEGIN
    CREATE INDEX [IX_Terminals_BranchId] ON [MasterData].[Terminals] ([BranchId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimeZoneEntries_IanaId' AND object_id = OBJECT_ID(N'[MasterData].[TimeZoneEntries]'))
BEGIN
    CREATE UNIQUE INDEX [IX_TimeZoneEntries_IanaId] ON [MasterData].[TimeZoneEntries] ([IanaId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Warehouses_BranchId' AND object_id = OBJECT_ID(N'[MasterData].[Warehouses]'))
BEGIN
    CREATE INDEX [IX_Warehouses_BranchId] ON [MasterData].[Warehouses] ([BranchId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Barcodes_ProductVariantId' AND object_id = OBJECT_ID(N'[Catalog].[Barcodes]'))
BEGIN
    CREATE INDEX [IX_Barcodes_ProductVariantId] ON [Catalog].[Barcodes] ([ProductVariantId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Barcodes_Value' AND object_id = OBJECT_ID(N'[Catalog].[Barcodes]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Barcodes_Value] ON [Catalog].[Barcodes] ([Value] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductPrices_ProductVariantId' AND object_id = OBJECT_ID(N'[Catalog].[ProductPrices]'))
BEGIN
    CREATE INDEX [IX_ProductPrices_ProductVariantId] ON [Catalog].[ProductPrices] ([ProductVariantId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Sku' AND object_id = OBJECT_ID(N'[Catalog].[Products]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [Catalog].[Products] ([Sku] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductVariants_ProductId' AND object_id = OBJECT_ID(N'[Catalog].[ProductVariants]'))
BEGIN
    CREATE INDEX [IX_ProductVariants_ProductId] ON [Catalog].[ProductVariants] ([ProductId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductVariants_Sku' AND object_id = OBJECT_ID(N'[Catalog].[ProductVariants]'))
BEGIN
    CREATE UNIQUE INDEX [IX_ProductVariants_Sku] ON [Catalog].[ProductVariants] ([Sku] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UnitsOfMeasure_Code' AND object_id = OBJECT_ID(N'[Catalog].[UnitsOfMeasure]'))
BEGIN
    CREATE UNIQUE INDEX [IX_UnitsOfMeasure_Code] ON [Catalog].[UnitsOfMeasure] ([Code] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryTransactions_OccurredAtUtc' AND object_id = OBJECT_ID(N'[Inventory].[InventoryTransactions]'))
BEGIN
    CREATE INDEX [IX_InventoryTransactions_OccurredAtUtc] ON [Inventory].[InventoryTransactions] ([OccurredAtUtc] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryTransactions_WarehouseId' AND object_id = OBJECT_ID(N'[Inventory].[InventoryTransactions]'))
BEGIN
    CREATE INDEX [IX_InventoryTransactions_WarehouseId] ON [Inventory].[InventoryTransactions] ([WarehouseId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockAdjustments_WarehouseId' AND object_id = OBJECT_ID(N'[Inventory].[StockAdjustments]'))
BEGIN
    CREATE INDEX [IX_StockAdjustments_WarehouseId] ON [Inventory].[StockAdjustments] ([WarehouseId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarehouseStocks_ProductVariantId' AND object_id = OBJECT_ID(N'[Inventory].[WarehouseStocks]'))
BEGIN
    CREATE INDEX [IX_WarehouseStocks_ProductVariantId] ON [Inventory].[WarehouseStocks] ([ProductVariantId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarehouseStocks_WarehouseId_ProductVariantId' AND object_id = OBJECT_ID(N'[Inventory].[WarehouseStocks]'))
BEGIN
    CREATE UNIQUE INDEX [IX_WarehouseStocks_WarehouseId_ProductVariantId] ON [Inventory].[WarehouseStocks] ([WarehouseId] ASC, [ProductVariantId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActivityLogEntries_OccurredAtUtc' AND object_id = OBJECT_ID(N'[Restaurant].[ActivityLogEntries]'))
BEGIN
    CREATE INDEX [IX_ActivityLogEntries_OccurredAtUtc] ON [Restaurant].[ActivityLogEntries] ([OccurredAtUtc] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceSessions_BranchId' AND object_id = OBJECT_ID(N'[Restaurant].[AttendanceSessions]'))
BEGIN
    CREATE INDEX [IX_AttendanceSessions_BranchId] ON [Restaurant].[AttendanceSessions] ([BranchId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceSessions_PunchInAtUtc' AND object_id = OBJECT_ID(N'[Restaurant].[AttendanceSessions]'))
BEGIN
    CREATE INDEX [IX_AttendanceSessions_PunchInAtUtc] ON [Restaurant].[AttendanceSessions] ([PunchInAtUtc] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceSessions_Status' AND object_id = OBJECT_ID(N'[Restaurant].[AttendanceSessions]'))
BEGIN
    CREATE INDEX [IX_AttendanceSessions_Status] ON [Restaurant].[AttendanceSessions] ([Status] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceSessions_UserId' AND object_id = OBJECT_ID(N'[Restaurant].[AttendanceSessions]'))
BEGIN
    CREATE INDEX [IX_AttendanceSessions_UserId] ON [Restaurant].[AttendanceSessions] ([UserId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceSessions_UserId_Open' AND object_id = OBJECT_ID(N'[Restaurant].[AttendanceSessions]'))
BEGIN
    CREATE UNIQUE INDEX [IX_AttendanceSessions_UserId_Open] ON [Restaurant].[AttendanceSessions] ([UserId] ASC) WHERE ([PunchOutAtUtc] IS NULL);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BusinessDayCloses_BranchId_BusinessDate' AND object_id = OBJECT_ID(N'[Restaurant].[BusinessDayCloses]'))
BEGIN
    CREATE UNIQUE INDEX [IX_BusinessDayCloses_BranchId_BusinessDate] ON [Restaurant].[BusinessDayCloses] ([BranchId] ASC, [BusinessDate] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CashMovements_ShiftId' AND object_id = OBJECT_ID(N'[Restaurant].[CashMovements]'))
BEGIN
    CREATE INDEX [IX_CashMovements_ShiftId] ON [Restaurant].[CashMovements] ([ShiftId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerLedgerEntries_CustomerId' AND object_id = OBJECT_ID(N'[Restaurant].[CustomerLedgerEntries]'))
BEGIN
    CREATE INDEX [IX_CustomerLedgerEntries_CustomerId] ON [Restaurant].[CustomerLedgerEntries] ([CustomerId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerLedgerEntries_ShiftId' AND object_id = OBJECT_ID(N'[Restaurant].[CustomerLedgerEntries]'))
BEGIN
    CREATE INDEX [IX_CustomerLedgerEntries_ShiftId] ON [Restaurant].[CustomerLedgerEntries] ([ShiftId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerPaymentAllocations_CustomerId' AND object_id = OBJECT_ID(N'[Restaurant].[CustomerPaymentAllocations]'))
BEGIN
    CREATE INDEX [IX_CustomerPaymentAllocations_CustomerId] ON [Restaurant].[CustomerPaymentAllocations] ([CustomerId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerPaymentAllocations_CustomerLedgerEntryId' AND object_id = OBJECT_ID(N'[Restaurant].[CustomerPaymentAllocations]'))
BEGIN
    CREATE INDEX [IX_CustomerPaymentAllocations_CustomerLedgerEntryId] ON [Restaurant].[CustomerPaymentAllocations] ([CustomerLedgerEntryId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerPaymentAllocations_OrderId' AND object_id = OBJECT_ID(N'[Restaurant].[CustomerPaymentAllocations]'))
BEGIN
    CREATE INDEX [IX_CustomerPaymentAllocations_OrderId] ON [Restaurant].[CustomerPaymentAllocations] ([OrderId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_Code' AND object_id = OBJECT_ID(N'[Restaurant].[Customers]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_Code] ON [Restaurant].[Customers] ([Code] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DailySalesSequences_WarehouseId_Date' AND object_id = OBJECT_ID(N'[Restaurant].[DailySalesSequences]'))
BEGIN
    CREATE UNIQUE INDEX [IX_DailySalesSequences_WarehouseId_Date] ON [Restaurant].[DailySalesSequences] ([WarehouseId] ASC, [Date] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DiningAreas_BranchId' AND object_id = OBJECT_ID(N'[Restaurant].[DiningAreas]'))
BEGIN
    CREATE INDEX [IX_DiningAreas_BranchId] ON [Restaurant].[DiningAreas] ([BranchId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Discounts_OrderId' AND object_id = OBJECT_ID(N'[Restaurant].[Discounts]'))
BEGIN
    CREATE INDEX [IX_Discounts_OrderId] ON [Restaurant].[Discounts] ([OrderId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KitchenTickets_OrderId' AND object_id = OBJECT_ID(N'[Restaurant].[KitchenTickets]'))
BEGIN
    CREATE INDEX [IX_KitchenTickets_OrderId] ON [Restaurant].[KitchenTickets] ([OrderId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KitchenTickets_Status' AND object_id = OBJECT_ID(N'[Restaurant].[KitchenTickets]'))
BEGIN
    CREATE INDEX [IX_KitchenTickets_Status] ON [Restaurant].[KitchenTickets] ([Status] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderLines_OrderId' AND object_id = OBJECT_ID(N'[Restaurant].[OrderLines]'))
BEGIN
    CREATE INDEX [IX_OrderLines_OrderId] ON [Restaurant].[OrderLines] ([OrderId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_CustomerId' AND object_id = OBJECT_ID(N'[Restaurant].[Orders]'))
BEGIN
    CREATE INDEX [IX_Orders_CustomerId] ON [Restaurant].[Orders] ([CustomerId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_OrderNumber' AND object_id = OBJECT_ID(N'[Restaurant].[Orders]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [Restaurant].[Orders] ([OrderNumber] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_Status' AND object_id = OBJECT_ID(N'[Restaurant].[Orders]'))
BEGIN
    CREATE INDEX [IX_Orders_Status] ON [Restaurant].[Orders] ([Status] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_TableId' AND object_id = OBJECT_ID(N'[Restaurant].[Orders]'))
BEGIN
    CREATE INDEX [IX_Orders_TableId] ON [Restaurant].[Orders] ([TableId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_OrderId' AND object_id = OBJECT_ID(N'[Restaurant].[Payments]'))
BEGIN
    CREATE INDEX [IX_Payments_OrderId] ON [Restaurant].[Payments] ([OrderId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_ShiftId' AND object_id = OBJECT_ID(N'[Restaurant].[Payments]'))
BEGIN
    CREATE INDEX [IX_Payments_ShiftId] ON [Restaurant].[Payments] ([ShiftId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_QuickOrderTemplateItems_TemplateId' AND object_id = OBJECT_ID(N'[Restaurant].[QuickOrderTemplateItems]'))
BEGIN
    CREATE INDEX [IX_QuickOrderTemplateItems_TemplateId] ON [Restaurant].[QuickOrderTemplateItems] ([TemplateId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_QuickOrderTemplates_IsActive' AND object_id = OBJECT_ID(N'[Restaurant].[QuickOrderTemplates]'))
BEGIN
    CREATE INDEX [IX_QuickOrderTemplates_IsActive] ON [Restaurant].[QuickOrderTemplates] ([IsActive] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecommendationRules_IsActive' AND object_id = OBJECT_ID(N'[Restaurant].[RecommendationRules]'))
BEGIN
    CREATE INDEX [IX_RecommendationRules_IsActive] ON [Restaurant].[RecommendationRules] ([IsActive] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecommendationRules_Priority' AND object_id = OBJECT_ID(N'[Restaurant].[RecommendationRules]'))
BEGIN
    CREATE INDEX [IX_RecommendationRules_Priority] ON [Restaurant].[RecommendationRules] ([Priority] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecommendationRules_ProductId' AND object_id = OBJECT_ID(N'[Restaurant].[RecommendationRules]'))
BEGIN
    CREATE INDEX [IX_RecommendationRules_ProductId] ON [Restaurant].[RecommendationRules] ([ProductId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecommendationRules_RecommendedVariantId' AND object_id = OBJECT_ID(N'[Restaurant].[RecommendationRules]'))
BEGIN
    CREATE INDEX [IX_RecommendationRules_RecommendedVariantId] ON [Restaurant].[RecommendationRules] ([RecommendedVariantId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ServiceCharges_OrderId' AND object_id = OBJECT_ID(N'[Restaurant].[ServiceCharges]'))
BEGIN
    CREATE INDEX [IX_ServiceCharges_OrderId] ON [Restaurant].[ServiceCharges] ([OrderId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shifts_CashierId_Active' AND object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Shifts_CashierId_Active] ON [Restaurant].[Shifts] ([CashierId] ASC) WHERE ([Status]='Open');
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shifts_ShiftNumber' AND object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Shifts_ShiftNumber] ON [Restaurant].[Shifts] ([ShiftNumber] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shifts_Status' AND object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
BEGIN
    CREATE INDEX [IX_Shifts_Status] ON [Restaurant].[Shifts] ([Status] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shifts_TerminalId_Active' AND object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Shifts_TerminalId_Active] ON [Restaurant].[Shifts] ([TerminalId] ASC) WHERE ([Status]='Open');
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SuggestionEvents_Kind' AND object_id = OBJECT_ID(N'[Restaurant].[SuggestionEvents]'))
BEGIN
    CREATE INDEX [IX_SuggestionEvents_Kind] ON [Restaurant].[SuggestionEvents] ([Kind] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SuggestionEvents_OccurredAtUtc' AND object_id = OBJECT_ID(N'[Restaurant].[SuggestionEvents]'))
BEGIN
    CREATE INDEX [IX_SuggestionEvents_OccurredAtUtc] ON [Restaurant].[SuggestionEvents] ([OccurredAtUtc] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SuggestionEvents_OrderId' AND object_id = OBJECT_ID(N'[Restaurant].[SuggestionEvents]'))
BEGIN
    CREATE INDEX [IX_SuggestionEvents_OrderId] ON [Restaurant].[SuggestionEvents] ([OrderId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SuggestionEvents_VariantId' AND object_id = OBJECT_ID(N'[Restaurant].[SuggestionEvents]'))
BEGIN
    CREATE INDEX [IX_SuggestionEvents_VariantId] ON [Restaurant].[SuggestionEvents] ([VariantId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tables_DiningAreaId' AND object_id = OBJECT_ID(N'[Restaurant].[Tables]'))
BEGIN
    CREATE INDEX [IX_Tables_DiningAreaId] ON [Restaurant].[Tables] ([DiningAreaId] ASC);
END
GO


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tables_DiningAreaId_Code' AND object_id = OBJECT_ID(N'[Restaurant].[Tables]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Tables_DiningAreaId_Code] ON [Restaurant].[Tables] ([DiningAreaId] ASC, [Code] ASC);
END
GO


-- DATA MIGRATION


PRINT 'Migrating data for [Authentication].[LoginAttempts]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Authentication].[LoginAttempts] ([Id], [AttemptedIdentifier], [UserId], [Outcome], [IpAddress], [OccurredAtUtc])
SELECT [Id], [AttemptedIdentifier], [UserId], [Outcome], [IpAddress], [OccurredAtUtc] FROM [Clovent_Authentication].[Authentication].[LoginAttempts];
GO


PRINT 'Migrating data for [Authentication].[RefreshSessions]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Authentication].[RefreshSessions] ([Id], [SessionId], [IssuedAtUtc], [ExpiresAtUtc], [Status])
SELECT [Id], [SessionId], [IssuedAtUtc], [ExpiresAtUtc], [Status] FROM [Clovent_Authentication].[Authentication].[RefreshSessions];
GO


PRINT 'Migrating data for [Authentication].[Sessions]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Authentication].[Sessions] ([Id], [UserId], [IpAddress], [IdleTimeout], [StartedAtUtc], [LastActivityAtUtc], [ExpiresAtUtc], [Status])
SELECT [Id], [UserId], [IpAddress], [IdleTimeout], [StartedAtUtc], [LastActivityAtUtc], [ExpiresAtUtc], [Status] FROM [Clovent_Authentication].[Authentication].[Sessions];
GO


PRINT 'Migrating data for [Authentication].[UserCredentials]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Authentication].[UserCredentials] ([Id], [UserId], [PasswordHash], [PinHash], [SecurityStamp], [PasswordHistory], [FailedAttempts])
SELECT [Id], [UserId], [PasswordHash], [PinHash], [SecurityStamp], [PasswordHistory], [FailedAttempts] FROM [Clovent_Authentication].[Authentication].[UserCredentials];
GO


PRINT 'Migrating data for [Identity].[Branches]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Identity].[Branches] ([Id], [CompanyId], [Name], [Address], [Status], [CreatedAtUtc])
SELECT [Id], [CompanyId], [Name], [Address], [Status], [CreatedAtUtc] FROM [Clovent_Identity].[Identity].[Branches];
GO


PRINT 'Migrating data for [Identity].[Companies]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Identity].[Companies] ([Id], [OrganizationId], [Name], [TaxId], [Status], [BranchIds], [CreatedAtUtc])
SELECT [Id], [OrganizationId], [Name], [TaxId], [Status], [BranchIds], [CreatedAtUtc] FROM [Clovent_Identity].[Identity].[Companies];
GO


PRINT 'Migrating data for [Identity].[Organizations]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Identity].[Organizations] ([Id], [Name], [TaxId], [Status], [CompanyIds], [CreatedAtUtc])
SELECT [Id], [Name], [TaxId], [Status], [CompanyIds], [CreatedAtUtc] FROM [Clovent_Identity].[Identity].[Organizations];
GO


PRINT 'Migrating data for [Identity].[Permissions]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Identity].[Permissions] ([Id], [Code], [Description], [CreatedAtUtc])
SELECT [Id], [Code], [Description], [CreatedAtUtc] FROM [Clovent_Identity].[Identity].[Permissions];
GO


PRINT 'Migrating data for [Identity].[Roles]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Identity].[Roles] ([Id], [Name], [PermissionIds], [CreatedAtUtc])
SELECT [Id], [Name], [PermissionIds], [CreatedAtUtc] FROM [Clovent_Identity].[Identity].[Roles];
GO


PRINT 'Migrating data for [Identity].[Users]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Identity].[Users] ([Id], [Email], [UserName], [DisplayName], [Status], [RoleIds], [CreatedAtUtc], [BranchId], [CompanyId])
SELECT [Id], [Email], [UserName], [DisplayName], [Status], [RoleIds], [CreatedAtUtc], [BranchId], [CompanyId] FROM [Clovent_Identity].[Identity].[Users];
GO


PRINT 'Migrating data for [MasterData].[BusinessSettings]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[MasterData].[BusinessSettings] ([Id], [OrganizationId], [DefaultCurrencyId], [DefaultLanguageId], [DefaultTimeZoneId], [DefaultFiscalYearId], [DateFormat], [CreatedAtUtc], [UpdatedAtUtc])
SELECT [Id], [OrganizationId], [DefaultCurrencyId], [DefaultLanguageId], [DefaultTimeZoneId], [DefaultFiscalYearId], [DateFormat], [CreatedAtUtc], [UpdatedAtUtc] FROM [Clovent_MasterData].[MasterData].[BusinessSettings];
GO


PRINT 'Migrating data for [MasterData].[Currencies]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[MasterData].[Currencies] ([Id], [Code], [Name], [Symbol], [DecimalPlaces], [Status], [CreatedAtUtc])
SELECT [Id], [Code], [Name], [Symbol], [DecimalPlaces], [Status], [CreatedAtUtc] FROM [Clovent_MasterData].[MasterData].[Currencies];
GO


PRINT 'Migrating data for [MasterData].[Departments]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[MasterData].[Departments] ([Id], [BranchId], [Name], [Status], [CreatedAtUtc])
SELECT [Id], [BranchId], [Name], [Status], [CreatedAtUtc] FROM [Clovent_MasterData].[MasterData].[Departments];
GO


PRINT 'Migrating data for [MasterData].[FiscalYears]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[MasterData].[FiscalYears] ([Id], [OrganizationId], [Name], [StartDate], [EndDate], [Status], [CreatedAtUtc])
SELECT [Id], [OrganizationId], [Name], [StartDate], [EndDate], [Status], [CreatedAtUtc] FROM [Clovent_MasterData].[MasterData].[FiscalYears];
GO


PRINT 'Migrating data for [MasterData].[Languages]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[MasterData].[Languages] ([Id], [Code], [Name], [NativeName], [Status], [CreatedAtUtc])
SELECT [Id], [Code], [Name], [NativeName], [Status], [CreatedAtUtc] FROM [Clovent_MasterData].[MasterData].[Languages];
GO


PRINT 'Migrating data for [MasterData].[Terminals]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[MasterData].[Terminals] ([Id], [BranchId], [Name], [Code], [Status], [CreatedAtUtc])
SELECT [Id], [BranchId], [Name], [Code], [Status], [CreatedAtUtc] FROM [Clovent_MasterData].[MasterData].[Terminals];
GO


PRINT 'Migrating data for [MasterData].[TimeZoneEntries]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[MasterData].[TimeZoneEntries] ([Id], [IanaId], [DisplayName], [UtcOffsetMinutes], [Status], [CreatedAtUtc])
SELECT [Id], [IanaId], [DisplayName], [UtcOffsetMinutes], [Status], [CreatedAtUtc] FROM [Clovent_MasterData].[MasterData].[TimeZoneEntries];
GO


PRINT 'Migrating data for [MasterData].[Warehouses]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[MasterData].[Warehouses] ([Id], [BranchId], [Name], [Code], [Status], [CreatedAtUtc])
SELECT [Id], [BranchId], [Name], [Code], [Status], [CreatedAtUtc] FROM [Clovent_MasterData].[MasterData].[Warehouses];
GO


PRINT 'Migrating data for [Catalog].[Barcodes]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Catalog].[Barcodes] ([Id], [ProductVariantId], [Value], [IsPrimary], [Status], [CreatedAtUtc])
SELECT [Id], [ProductVariantId], [Value], [IsPrimary], [Status], [CreatedAtUtc] FROM [Clovent_Catalog].[Catalog].[Barcodes];
GO


PRINT 'Migrating data for [Catalog].[Brands]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Catalog].[Brands] ([Id], [Name], [Status], [CreatedAtUtc])
SELECT [Id], [Name], [Status], [CreatedAtUtc] FROM [Clovent_Catalog].[Catalog].[Brands];
GO


PRINT 'Migrating data for [Catalog].[ProductCategories]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Catalog].[ProductCategories] ([Id], [Name], [ParentCategoryId], [Status], [CreatedAtUtc], [ColorHex], [SortOrder])
SELECT [Id], [Name], [ParentCategoryId], [Status], [CreatedAtUtc], [ColorHex], [SortOrder] FROM [Clovent_Catalog].[Catalog].[ProductCategories];
GO


PRINT 'Migrating data for [Catalog].[ProductGroups]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Catalog].[ProductGroups] ([Id], [Name], [Status], [CreatedAtUtc])
SELECT [Id], [Name], [Status], [CreatedAtUtc] FROM [Clovent_Catalog].[Catalog].[ProductGroups];
GO


PRINT 'Migrating data for [Catalog].[ProductPrices]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Catalog].[ProductPrices] ([Id], [ProductVariantId], [PriceType], [Amount], [CurrencyId], [EffectiveFromUtc], [Status], [CreatedAtUtc])
SELECT [Id], [ProductVariantId], [PriceType], [Amount], [CurrencyId], [EffectiveFromUtc], [Status], [CreatedAtUtc] FROM [Clovent_Catalog].[Catalog].[ProductPrices];
GO


PRINT 'Migrating data for [Catalog].[Products]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Catalog].[Products] ([Id], [Name], [Sku], [CategoryId], [GroupId], [BrandId], [BaseUnitOfMeasureId], [TaxConfiguration], [Status], [CreatedAtUtc], [ItemType])
SELECT [Id], [Name], [Sku], [CategoryId], [GroupId], [BrandId], [BaseUnitOfMeasureId], [TaxConfiguration], [Status], [CreatedAtUtc], [ItemType] FROM [Clovent_Catalog].[Catalog].[Products];
GO


PRINT 'Migrating data for [Catalog].[ProductVariants]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Catalog].[ProductVariants] ([Id], [ProductId], [Name], [Sku], [UnitOfMeasureId], [Status], [CreatedAtUtc], [SortOrder], [IsAvailable])
SELECT [Id], [ProductId], [Name], [Sku], [UnitOfMeasureId], [Status], [CreatedAtUtc], [SortOrder], [IsAvailable] FROM [Clovent_Catalog].[Catalog].[ProductVariants];
GO


PRINT 'Migrating data for [Catalog].[UnitsOfMeasure]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Catalog].[UnitsOfMeasure] ([Id], [Code], [Name], [Status], [CreatedAtUtc])
SELECT [Id], [Code], [Name], [Status], [CreatedAtUtc] FROM [Clovent_Catalog].[Catalog].[UnitsOfMeasure];
GO


PRINT 'Migrating data for [Inventory].[InventoryTransactions]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Inventory].[InventoryTransactions] ([Id], [WarehouseId], [ProductVariantId], [TransactionType], [Quantity], [ReferenceType], [ReferenceId], [Notes], [OccurredAtUtc])
SELECT [Id], [WarehouseId], [ProductVariantId], [TransactionType], [Quantity], [ReferenceType], [ReferenceId], [Notes], [OccurredAtUtc] FROM [Clovent_Inventory].[Inventory].[InventoryTransactions];
GO


PRINT 'Migrating data for [Inventory].[StockAdjustments]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Inventory].[StockAdjustments] ([Id], [WarehouseId], [ProductVariantId], [AdjustmentType], [Quantity], [Reason], [Status], [CreatedAtUtc], [AppliedAtUtc])
SELECT [Id], [WarehouseId], [ProductVariantId], [AdjustmentType], [Quantity], [Reason], [Status], [CreatedAtUtc], [AppliedAtUtc] FROM [Clovent_Inventory].[Inventory].[StockAdjustments];
GO


PRINT 'Migrating data for [Inventory].[StockTransfers]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Inventory].[StockTransfers] ([Id], [SourceWarehouseId], [DestinationWarehouseId], [ProductVariantId], [Quantity], [Status], [CreatedAtUtc], [CompletedAtUtc])
SELECT [Id], [SourceWarehouseId], [DestinationWarehouseId], [ProductVariantId], [Quantity], [Status], [CreatedAtUtc], [CompletedAtUtc] FROM [Clovent_Inventory].[Inventory].[StockTransfers];
GO


PRINT 'Migrating data for [Inventory].[WarehouseStocks]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Inventory].[WarehouseStocks] ([Id], [WarehouseId], [ProductVariantId], [QuantityOnHand], [QuantityReserved], [MinimumStock], [MaximumStock], [AllowNegativeStock], [CreatedAtUtc], [UpdatedAtUtc])
SELECT [Id], [WarehouseId], [ProductVariantId], [QuantityOnHand], [QuantityReserved], [MinimumStock], [MaximumStock], [AllowNegativeStock], [CreatedAtUtc], [UpdatedAtUtc] FROM [Clovent_Inventory].[Inventory].[WarehouseStocks];
GO


PRINT 'Migrating data for [Restaurant].[ActivityLogEntries]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[ActivityLogEntries] ([Id], [Action], [Details], [PerformedBy], [MachineName], [OccurredAtUtc])
SELECT [Id], [Action], [Details], [PerformedBy], [MachineName], [OccurredAtUtc] FROM [Clovent_Restaurant].[Restaurant].[ActivityLogEntries];
GO


PRINT 'Migrating data for [Restaurant].[AttendanceSessions]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[AttendanceSessions] ([Id], [UserId], [UserName], [BranchId], [BranchName], [PunchInTerminalId], [PunchOutTerminalId], [PunchInAtUtc], [PunchOutAtUtc], [Status], [Notes], [CreatedAtUtc], [UpdatedAtUtc])
SELECT [Id], [UserId], [UserName], [BranchId], [BranchName], [PunchInTerminalId], [PunchOutTerminalId], [PunchInAtUtc], [PunchOutAtUtc], [Status], [Notes], [CreatedAtUtc], [UpdatedAtUtc] FROM [Clovent_Restaurant].[Restaurant].[AttendanceSessions];
GO


PRINT 'Migrating data for [Restaurant].[BusinessDayCloses]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[BusinessDayCloses] ([Id], [BranchId], [BusinessDate], [ClosedAtUtc], [ClosedByUserId], [ClosedByUserName], [Status], [TotalSales], [CashSales], [CardSales], [OtherPayments], [Refunds], [Discounts], [Tax], [CashIn], [CashOut], [ShiftCount], [TotalShiftVariance], [OrderCount], [Notes], [CreatedAtUtc])
SELECT [Id], [BranchId], [BusinessDate], [ClosedAtUtc], [ClosedByUserId], [ClosedByUserName], [Status], [TotalSales], [CashSales], [CardSales], [OtherPayments], [Refunds], [Discounts], [Tax], [CashIn], [CashOut], [ShiftCount], [TotalShiftVariance], [OrderCount], [Notes], [CreatedAtUtc] FROM [Clovent_Restaurant].[Restaurant].[BusinessDayCloses];
GO


PRINT 'Migrating data for [Restaurant].[CashMovements]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[CashMovements] ([Id], [ShiftId], [Type], [Amount], [Reason], [UserId], [TimestampUtc], [Notes])
SELECT [Id], [ShiftId], [Type], [Amount], [Reason], [UserId], [TimestampUtc], [Notes] FROM [Clovent_Restaurant].[Restaurant].[CashMovements];
GO


PRINT 'Migrating data for [Restaurant].[ComboDecisions]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[ComboDecisions] ([WarehouseId], [Signature], [TemplateId], [UserId], [DecidedAtUtc], [DismissedUntilUtc], [Reason], [Version])
SELECT [WarehouseId], [Signature], [TemplateId], [UserId], [DecidedAtUtc], [DismissedUntilUtc], [Reason], [Version] FROM [Clovent_Restaurant].[Restaurant].[ComboDecisions];
GO


PRINT 'Migrating data for [Restaurant].[CustomerLedgerEntries]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[CustomerLedgerEntries] ([Id], [CustomerId], [Date], [Reference], [Description], [Debit], [Credit], [RunningBalance], [ShiftId], [PaymentMethod])
SELECT [Id], [CustomerId], [Date], [Reference], [Description], [Debit], [Credit], [RunningBalance], [ShiftId], [PaymentMethod] FROM [Clovent_Restaurant].[Restaurant].[CustomerLedgerEntries];
GO


PRINT 'Migrating data for [Restaurant].[CustomerPaymentAllocations]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[CustomerPaymentAllocations] ([Id], [CustomerId], [OrderId], [CustomerLedgerEntryId], [Amount], [AllocatedAtUtc], [Notes])
SELECT [Id], [CustomerId], [OrderId], [CustomerLedgerEntryId], [Amount], [AllocatedAtUtc], [Notes] FROM [Clovent_Restaurant].[Restaurant].[CustomerPaymentAllocations];
GO


PRINT 'Migrating data for [Restaurant].[Customers]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[Customers] ([Id], [Code], [Name], [MobileNumber], [Address], [Email], [OpeningBalance], [CreditLimit], [OutstandingBalance], [IsActive], [Notes], [CreatedAtUtc], [UpdatedAtUtc], [Mobile2], [Phone], [ShopNo], [IsDefault], [IsCreditAllowed])
SELECT [Id], [Code], [Name], [MobileNumber], [Address], [Email], [OpeningBalance], [CreditLimit], [OutstandingBalance], [IsActive], [Notes], [CreatedAtUtc], [UpdatedAtUtc], [Mobile2], [Phone], [ShopNo], [IsDefault], [IsCreditAllowed] FROM [Clovent_Restaurant].[Restaurant].[Customers];
GO


PRINT 'Migrating data for [Restaurant].[DailySalesSequences]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[DailySalesSequences] ([Id], [WarehouseId], [Date], [LastNumber])
SELECT [Id], [WarehouseId], [Date], [LastNumber] FROM [Clovent_Restaurant].[Restaurant].[DailySalesSequences];
GO


PRINT 'Migrating data for [Restaurant].[DiningAreas]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[DiningAreas] ([Id], [BranchId], [Name], [Status], [CreatedAtUtc])
SELECT [Id], [BranchId], [Name], [Status], [CreatedAtUtc] FROM [Clovent_Restaurant].[Restaurant].[DiningAreas];
GO


PRINT 'Migrating data for [Restaurant].[Discounts]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[Discounts] ([Id], [OrderId], [DiscountType], [Value], [Reason], [CreatedAtUtc])
SELECT [Id], [OrderId], [DiscountType], [Value], [Reason], [CreatedAtUtc] FROM [Clovent_Restaurant].[Restaurant].[Discounts];
GO


PRINT 'Migrating data for [Restaurant].[KitchenTickets]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[KitchenTickets] ([Id], [OrderId], [OrderLineIds], [Status], [CreatedAtUtc], [StartedAtUtc], [ReadyAtUtc], [ServedAtUtc])
SELECT [Id], [OrderId], [OrderLineIds], [Status], [CreatedAtUtc], [StartedAtUtc], [ReadyAtUtc], [ServedAtUtc] FROM [Clovent_Restaurant].[Restaurant].[KitchenTickets];
GO


PRINT 'Migrating data for [Restaurant].[OrderLines]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[OrderLines] ([Id], [OrderId], [ProductVariantId], [Quantity], [UnitPrice], [TaxRatePercentage], [TaxIsInclusive], [Notes], [IsVoided], [CreatedAtUtc], [IsPriceOverridden], [OriginalUnitPrice], [PriceOverriddenAtUtc], [PriceOverriddenBy], [PriceOverrideReason])
SELECT [Id], [OrderId], [ProductVariantId], [Quantity], [UnitPrice], [TaxRatePercentage], [TaxIsInclusive], [Notes], [IsVoided], [CreatedAtUtc], [IsPriceOverridden], [OriginalUnitPrice], [PriceOverriddenAtUtc], [PriceOverriddenBy], [PriceOverrideReason] FROM [Clovent_Restaurant].[Restaurant].[OrderLines];
GO


PRINT 'Migrating data for [Restaurant].[OrderNumberSequences]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[OrderNumberSequences] ([Id], [Prefix], [NextNumber])
SELECT [Id], [Prefix], [NextNumber] FROM [Clovent_Restaurant].[Restaurant].[OrderNumberSequences];
GO


PRINT 'Migrating data for [Restaurant].[Orders]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[Orders] ([Id], [OrderNumber], [OrderType], [Status], [TableId], [WarehouseId], [Notes], [CustomerNotes], [OrderLineIds], [DiscountIds], [ServiceChargeIds], [PaymentIds], [CreatedAtUtc], [UpdatedAtUtc], [DailySalesNumber], [CustomerId], [OrderSource], [DeliveryStatus], [DeliveryCustomerName], [DeliveryPhone], [DeliveryAddress], [DeliveryNotes], [DeliveryFee], [RiderName], [RiderPhone])
SELECT [Id], [OrderNumber], [OrderType], [Status], [TableId], [WarehouseId], [Notes], [CustomerNotes], [OrderLineIds], [DiscountIds], [ServiceChargeIds], [PaymentIds], [CreatedAtUtc], [UpdatedAtUtc], [DailySalesNumber], [CustomerId], [OrderSource], [DeliveryStatus], [DeliveryCustomerName], [DeliveryPhone], [DeliveryAddress], [DeliveryNotes], [DeliveryFee], [RiderName], [RiderPhone] FROM [Clovent_Restaurant].[Restaurant].[Orders];
GO


PRINT 'Migrating data for [Restaurant].[PaymentMethods]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[PaymentMethods] ([Id], [Name], [Status], [CreatedAtUtc])
SELECT [Id], [Name], [Status], [CreatedAtUtc] FROM [Clovent_Restaurant].[Restaurant].[PaymentMethods];
GO


PRINT 'Migrating data for [Restaurant].[Payments]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[Payments] ([Id], [OrderId], [PaymentMethodId], [Amount], [IsVoided], [CreatedAtUtc], [ShiftId])
SELECT [Id], [OrderId], [PaymentMethodId], [Amount], [IsVoided], [CreatedAtUtc], [ShiftId] FROM [Clovent_Restaurant].[Restaurant].[Payments];
GO


PRINT 'Migrating data for [Restaurant].[QuickOrderTemplateItems]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[QuickOrderTemplateItems] ([Id], [TemplateId], [VariantId], [Quantity], [TemplateUnitPrice])
SELECT [Id], [TemplateId], [VariantId], [Quantity], [TemplateUnitPrice] FROM [Clovent_Restaurant].[Restaurant].[QuickOrderTemplateItems];
GO


PRINT 'Migrating data for [Restaurant].[QuickOrderTemplates]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[QuickOrderTemplates] ([Id], [Name], [Description], [IsActive], [DisplayOrder], [CreatedAtUtc], [UpdatedAtUtc], [WarehouseId])
SELECT [Id], [Name], [Description], [IsActive], [DisplayOrder], [CreatedAtUtc], [UpdatedAtUtc], [WarehouseId] FROM [Clovent_Restaurant].[Restaurant].[QuickOrderTemplates];
GO


PRINT 'Migrating data for [Restaurant].[RecommendationRules]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[RecommendationRules] ([Id], [ProductId], [RecommendedVariantId], [Priority], [IsActive], [StartTime], [EndTime], [DaysOfWeek], [Notes], [CreatedAtUtc], [UpdatedAtUtc])
SELECT [Id], [ProductId], [RecommendedVariantId], [Priority], [IsActive], [StartTime], [EndTime], [DaysOfWeek], [Notes], [CreatedAtUtc], [UpdatedAtUtc] FROM [Clovent_Restaurant].[Restaurant].[RecommendationRules];
GO


PRINT 'Migrating data for [Restaurant].[ServiceCharges]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[ServiceCharges] ([Id], [OrderId], [ServiceChargeType], [Value], [Reason], [CreatedAtUtc])
SELECT [Id], [OrderId], [ServiceChargeType], [Value], [Reason], [CreatedAtUtc] FROM [Clovent_Restaurant].[Restaurant].[ServiceCharges];
GO


PRINT 'Migrating data for [Restaurant].[Shifts]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[Shifts] ([Id], [ShiftNumber], [BranchId], [WarehouseId], [TerminalId], [CashierId], [CashierName], [OpenedAtUtc], [ClosedAtUtc], [Status], [StartingCash], [ExpectedCash], [CountedCash], [CashVariance], [VarianceReason], [Notes], [CreatedAtUtc], [UpdatedAtUtc])
SELECT [Id], [ShiftNumber], [BranchId], [WarehouseId], [TerminalId], [CashierId], [CashierName], [OpenedAtUtc], [ClosedAtUtc], [Status], [StartingCash], [ExpectedCash], [CountedCash], [CashVariance], [VarianceReason], [Notes], [CreatedAtUtc], [UpdatedAtUtc] FROM [Clovent_Restaurant].[Restaurant].[Shifts];
GO


PRINT 'Migrating data for [Restaurant].[SuggestionEvents]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[SuggestionEvents] ([Id], [OrderId], [VariantId], [TriggerVariantId], [Kind], [OrderLineId], [AcceptedQuantity], [AcceptedUnitAmount], [OccurredAtUtc])
SELECT [Id], [OrderId], [VariantId], [TriggerVariantId], [Kind], [OrderLineId], [AcceptedQuantity], [AcceptedUnitAmount], [OccurredAtUtc] FROM [Clovent_Restaurant].[Restaurant].[SuggestionEvents];
GO


PRINT 'Migrating data for [Restaurant].[Tables]...';
INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[Tables] ([Id], [DiningAreaId], [Code], [Capacity], [Status], [OccupancyStatus], [CreatedAtUtc], [Name])
SELECT [Id], [DiningAreaId], [Code], [Capacity], [Status], [OccupancyStatus], [CreatedAtUtc], [Name] FROM [Clovent_Restaurant].[Restaurant].[Tables];
GO


-- EF MIGRATION HISTORIES


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Authentication' AND t.name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [Authentication].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory_Authentication] PRIMARY KEY ([MigrationId])
    );
END
GO

INSERT INTO [Clovent_BusinessOperatingSystem].[Authentication].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT [MigrationId], [ProductVersion] FROM [Clovent_Authentication].[dbo].[__EFMigrationsHistory];
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Identity' AND t.name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [Identity].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory_Identity] PRIMARY KEY ([MigrationId])
    );
END
GO

INSERT INTO [Clovent_BusinessOperatingSystem].[Identity].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT [MigrationId], [ProductVersion] FROM [Clovent_Identity].[dbo].[__EFMigrationsHistory];
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [MasterData].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory_MasterData] PRIMARY KEY ([MigrationId])
    );
END
GO

INSERT INTO [Clovent_BusinessOperatingSystem].[MasterData].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT [MigrationId], [ProductVersion] FROM [Clovent_MasterData].[dbo].[__EFMigrationsHistory];
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Catalog' AND t.name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [Catalog].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory_Catalog] PRIMARY KEY ([MigrationId])
    );
END
GO

INSERT INTO [Clovent_BusinessOperatingSystem].[Catalog].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT [MigrationId], [ProductVersion] FROM [Clovent_Catalog].[dbo].[__EFMigrationsHistory];
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Inventory' AND t.name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [Inventory].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory_Inventory] PRIMARY KEY ([MigrationId])
    );
END
GO

INSERT INTO [Clovent_BusinessOperatingSystem].[Inventory].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT [MigrationId], [ProductVersion] FROM [Clovent_Inventory].[dbo].[__EFMigrationsHistory];
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Restaurant' AND t.name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [Restaurant].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory_Restaurant] PRIMARY KEY ([MigrationId])
    );
END
GO

INSERT INTO [Clovent_BusinessOperatingSystem].[Restaurant].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT [MigrationId], [ProductVersion] FROM [Clovent_Restaurant].[dbo].[__EFMigrationsHistory];
GO


-- FOREIGN KEYS


IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CashMovements_Shifts_ShiftId' AND parent_object_id = OBJECT_ID(N'[Restaurant].[CashMovements]'))
BEGIN
    ALTER TABLE [Restaurant].[CashMovements] ADD CONSTRAINT [FK_CashMovements_Shifts_ShiftId] 
    FOREIGN KEY ([ShiftId]) REFERENCES [Restaurant].[Shifts] ([Id]) ON DELETE CASCADE;
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_QuickOrderTemplateItems_QuickOrderTemplates_TemplateId' AND parent_object_id = OBJECT_ID(N'[Restaurant].[QuickOrderTemplateItems]'))
BEGIN
    ALTER TABLE [Restaurant].[QuickOrderTemplateItems] ADD CONSTRAINT [FK_QuickOrderTemplateItems_QuickOrderTemplates_TemplateId] 
    FOREIGN KEY ([TemplateId]) REFERENCES [Restaurant].[QuickOrderTemplates] ([Id]) ON DELETE CASCADE;
END
GO


PRINT 'Verifying all foreign key constraints...';
EXEC sp_MSforeachtable @command1="ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL";
GO
