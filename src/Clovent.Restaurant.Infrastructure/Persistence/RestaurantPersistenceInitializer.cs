using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations for <see cref="RestaurantDbContext"/> at startup.</summary>
public sealed class RestaurantPersistenceInitializer(RestaurantDbContext dbContext) : IPersistenceInitializer
{
    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        // Ensure ShiftId column exists on [Restaurant].[Payments] and that Shifts and CashMovements tables
        // exist in case the migration was previously recorded in __EFMigrationsHistory when its Up() method was empty.
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
            BEGIN
                CREATE TABLE [Restaurant].[Shifts] (
                    [Id] uniqueidentifier NOT NULL,
                    [BranchId] uniqueidentifier NOT NULL,
                    [CashierId] uniqueidentifier NOT NULL,
                    [CashierName] nvarchar(150) NOT NULL,
                    [CashVariance] decimal(18,2) NOT NULL,
                    [ClosedAtUtc] datetimeoffset NULL,
                    [CountedCash] decimal(18,2) NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [ExpectedCash] decimal(18,2) NOT NULL,
                    [Notes] nvarchar(1000) NULL,
                    [OpenedAtUtc] datetimeoffset NOT NULL,
                    [ShiftNumber] int NOT NULL,
                    [StartingCash] decimal(18,2) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [TerminalId] uniqueidentifier NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL,
                    [VarianceReason] nvarchar(500) NULL,
                    [WarehouseId] uniqueidentifier NOT NULL,
                    CONSTRAINT [PK_Shifts] PRIMARY KEY ([Id])
                );

                CREATE UNIQUE INDEX [IX_Shifts_ShiftNumber] ON [Restaurant].[Shifts] ([ShiftNumber]);
                CREATE INDEX [IX_Shifts_CashierId] ON [Restaurant].[Shifts] ([CashierId]);
                CREATE INDEX [IX_Shifts_Status] ON [Restaurant].[Shifts] ([Status]);
                CREATE INDEX [IX_Shifts_TerminalId] ON [Restaurant].[Shifts] ([TerminalId]);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[CashMovements]'))
            BEGIN
                CREATE TABLE [Restaurant].[CashMovements] (
                    [Id] uniqueidentifier NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [Notes] nvarchar(500) NULL,
                    [Reason] nvarchar(250) NOT NULL,
                    [ShiftId] uniqueidentifier NOT NULL,
                    [TimestampUtc] datetimeoffset NOT NULL,
                    [Type] nvarchar(20) NOT NULL,
                    [UserId] uniqueidentifier NOT NULL,
                    CONSTRAINT [PK_CashMovements] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_CashMovements_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Restaurant].[Shifts] ([Id]) ON DELETE CASCADE
                );

                CREATE INDEX [IX_CashMovements_ShiftId] ON [Restaurant].[CashMovements] ([ShiftId]);
            END

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

                CREATE UNIQUE INDEX [IX_AttendanceSessions_UserId_Open] ON [Restaurant].[AttendanceSessions] ([UserId]) WHERE [PunchOutAtUtc] IS NULL;
                CREATE INDEX [IX_AttendanceSessions_UserId] ON [Restaurant].[AttendanceSessions] ([UserId]);
                CREATE INDEX [IX_AttendanceSessions_BranchId] ON [Restaurant].[AttendanceSessions] ([BranchId]);
                CREATE INDEX [IX_AttendanceSessions_Status] ON [Restaurant].[AttendanceSessions] ([Status]);
                CREATE INDEX [IX_AttendanceSessions_PunchInAtUtc] ON [Restaurant].[AttendanceSessions] ([PunchInAtUtc]);
            END

            IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[Payments]'))
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[Restaurant].[Payments]') 
                    AND name = 'ShiftId'
                )
                BEGIN
                    ALTER TABLE [Restaurant].[Payments] ADD [ShiftId] UNIQUEIDENTIFIER NULL;
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes 
                    WHERE name = 'IX_Payments_ShiftId' 
                    AND object_id = OBJECT_ID(N'[Restaurant].[Payments]')
                )
                BEGIN
                    CREATE INDEX [IX_Payments_ShiftId] ON [Restaurant].[Payments] ([ShiftId]);
                END
            END

            IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[Customers]'))
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Restaurant].[Customers]')
                    AND name = 'IsDefault'
                )
                BEGIN
                    ALTER TABLE [Restaurant].[Customers] ADD [IsDefault] bit NOT NULL CONSTRAINT [DF_Customers_IsDefault] DEFAULT 0;
                END

                -- Idempotently seed Walk-in Customer if code C000 does not exist
                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[Customers] WHERE [Code] = 'C000')
                BEGIN
                    INSERT INTO [Restaurant].[Customers] (
                        [Id], [Code], [Name], [MobileNumber], [Address], [Email],
                        [OpeningBalance], [CreditLimit], [OutstandingBalance], [IsActive],
                        [IsDefault], [Notes], [CreatedAtUtc], [UpdatedAtUtc], [ShopNo], [Mobile2], [Phone]
                    ) VALUES (
                        NEWID(), 'C000', 'Walk-in Customer', '-', 'Counter', NULL,
                        0.00, 0.00, 0.00, 1,
                        1, 'System default counter customer', SYSUTCDATETIME(), SYSUTCDATETIME(), NULL, NULL, NULL
                    );
                END

                -- Ensure exactly one active customer is marked default if none is currently marked default
                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[Customers] WHERE [IsDefault] = 1 AND [IsActive] = 1)
                BEGIN
                    IF EXISTS (SELECT 1 FROM [Restaurant].[Customers] WHERE [Code] = 'C000' AND [IsActive] = 1)
                    BEGIN
                        UPDATE [Restaurant].[Customers] SET [IsDefault] = 1 WHERE [Code] = 'C000';
                    END
                    ELSE
                    BEGIN
                        UPDATE TOP (1) [Restaurant].[Customers] SET [IsDefault] = 1 WHERE [IsActive] = 1;
                    END
                END
            END

            -- Idempotently seed real Pakistani restaurant recommendation rules
            IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[RecommendationRules]'))
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[RecommendationRules] WHERE [ProductId] = '7E23EAAD-1BE3-427B-8E0E-0EF152AA49A7' AND [RecommendedVariantId] = 'A6EF7092-DAB1-4E73-A603-27001BD6D85D')
                BEGIN
                    INSERT INTO [Restaurant].[RecommendationRules] ([Id], [ProductId], [RecommendedVariantId], [Priority], [IsActive], [StartTime], [EndTime], [DaysOfWeek], [Notes], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (NEWID(), '7E23EAAD-1BE3-427B-8E0E-0EF152AA49A7', 'A6EF7092-DAB1-4E73-A603-27001BD6D85D', 1, 1, NULL, NULL, NULL, N'Suggest fresh salad with Chicken Biryani', SYSUTCDATETIME(), SYSUTCDATETIME());
                END

                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[RecommendationRules] WHERE [ProductId] = '7E23EAAD-1BE3-427B-8E0E-0EF152AA49A7' AND [RecommendedVariantId] = '62B73E75-129E-4489-9AD8-6C8C96FCB527')
                BEGIN
                    INSERT INTO [Restaurant].[RecommendationRules] ([Id], [ProductId], [RecommendedVariantId], [Priority], [IsActive], [StartTime], [EndTime], [DaysOfWeek], [Notes], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (NEWID(), '7E23EAAD-1BE3-427B-8E0E-0EF152AA49A7', '62B73E75-129E-4489-9AD8-6C8C96FCB527', 2, 1, NULL, NULL, NULL, N'Suggest cold beverage with Chicken Biryani', SYSUTCDATETIME(), SYSUTCDATETIME());
                END

                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[RecommendationRules] WHERE [ProductId] = 'E4D77145-B5A9-4640-B73C-D0F5E5DD1043' AND [RecommendedVariantId] = 'EA8D8743-D3D2-4E22-A8A7-97524FACE885')
                BEGIN
                    INSERT INTO [Restaurant].[RecommendationRules] ([Id], [ProductId], [RecommendedVariantId], [Priority], [IsActive], [StartTime], [EndTime], [DaysOfWeek], [Notes], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (NEWID(), 'E4D77145-B5A9-4640-B73C-D0F5E5DD1043', 'EA8D8743-D3D2-4E22-A8A7-97524FACE885', 1, 1, NULL, NULL, NULL, N'Suggest fresh Garlic Nan with Chicken Karahi', SYSUTCDATETIME(), SYSUTCDATETIME());
                END

                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[RecommendationRules] WHERE [ProductId] = '59CB9E1B-3EDB-4D4A-8F32-2074451C3056' AND [RecommendedVariantId] = 'EA8D8743-D3D2-4E22-A8A7-97524FACE885')
                BEGIN
                    INSERT INTO [Restaurant].[RecommendationRules] ([Id], [ProductId], [RecommendedVariantId], [Priority], [IsActive], [StartTime], [EndTime], [DaysOfWeek], [Notes], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (NEWID(), '59CB9E1B-3EDB-4D4A-8F32-2074451C3056', 'EA8D8743-D3D2-4E22-A8A7-97524FACE885', 1, 1, NULL, NULL, NULL, N'Suggest tandoori naan with Haleem', SYSUTCDATETIME(), SYSUTCDATETIME());
                END

                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[RecommendationRules] WHERE [ProductId] = '993F91B9-8295-41BD-9D37-6CB01F5F5F49' AND [RecommendedVariantId] = 'EA8D8743-D3D2-4E22-A8A7-97524FACE885')
                BEGIN
                    INSERT INTO [Restaurant].[RecommendationRules] ([Id], [ProductId], [RecommendedVariantId], [Priority], [IsActive], [StartTime], [EndTime], [DaysOfWeek], [Notes], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (NEWID(), '993F91B9-8295-41BD-9D37-6CB01F5F5F49', 'EA8D8743-D3D2-4E22-A8A7-97524FACE885', 1, 1, NULL, NULL, NULL, N'Suggest fresh Garlic Nan with Murgh Chanay breakfast', SYSUTCDATETIME(), SYSUTCDATETIME());
                END

                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[RecommendationRules] WHERE [ProductId] = '51BA892A-2788-4E95-B659-7AD109803E30' AND [RecommendedVariantId] = 'A6EF7092-DAB1-4E73-A603-27001BD6D85D')
                BEGIN
                    INSERT INTO [Restaurant].[RecommendationRules] ([Id], [ProductId], [RecommendedVariantId], [Priority], [IsActive], [StartTime], [EndTime], [DaysOfWeek], [Notes], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (NEWID(), '51BA892A-2788-4E95-B659-7AD109803E30', 'A6EF7092-DAB1-4E73-A603-27001BD6D85D', 1, 1, NULL, NULL, NULL, N'Suggest fresh salad with White Daal Mash', SYSUTCDATETIME(), SYSUTCDATETIME());
                END

                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[RecommendationRules] WHERE [ProductId] IS NULL AND [RecommendedVariantId] = 'A6EF7092-DAB1-4E73-A603-27001BD6D85D')
                BEGIN
                    INSERT INTO [Restaurant].[RecommendationRules] ([Id], [ProductId], [RecommendedVariantId], [Priority], [IsActive], [StartTime], [EndTime], [DaysOfWeek], [Notes], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (NEWID(), NULL, 'A6EF7092-DAB1-4E73-A603-27001BD6D85D', 10, 1, NULL, NULL, NULL, N'Suggest fresh salad for any meal order', SYSUTCDATETIME(), SYSUTCDATETIME());
                END
            END

            -- Idempotently seed real Pakistani restaurant quick order templates
            IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[QuickOrderTemplates]'))
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[QuickOrderTemplates] WHERE [Name] = 'Family Biryani Deal')
                BEGIN
                    DECLARE @T1 UNIQUEIDENTIFIER = NEWID();
                    INSERT INTO [Restaurant].[QuickOrderTemplates] ([Id], [Name], [Description], [IsActive], [DisplayOrder], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (@T1, 'Family Biryani Deal', N'4 Chicken Biryani + 2 Fresh Salads + 4 Cold Beverages', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

                    INSERT INTO [Restaurant].[QuickOrderTemplateItems] ([Id], [TemplateId], [VariantId], [Quantity], [TemplateUnitPrice])
                    VALUES 
                        (NEWID(), @T1, 'C28181E1-E80C-42D0-9402-1409621816F9', 4.000, 450.00),
                        (NEWID(), @T1, 'A6EF7092-DAB1-4E73-A603-27001BD6D85D', 2.000, 30.00),
                        (NEWID(), @T1, '62B73E75-129E-4489-9AD8-6C8C96FCB527', 4.000, 50.00);
                END

                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[QuickOrderTemplates] WHERE [Name] = 'Chicken Karahi Feast')
                BEGIN
                    DECLARE @T2 UNIQUEIDENTIFIER = NEWID();
                    INSERT INTO [Restaurant].[QuickOrderTemplates] ([Id], [Name], [Description], [IsActive], [DisplayOrder], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (@T2, 'Chicken Karahi Feast', N'1 Chicken Karahi + 4 Garlic Nan + 2 Fresh Salads + 2 Beverages', 1, 2, SYSUTCDATETIME(), SYSUTCDATETIME());

                    INSERT INTO [Restaurant].[QuickOrderTemplateItems] ([Id], [TemplateId], [VariantId], [Quantity], [TemplateUnitPrice])
                    VALUES 
                        (NEWID(), @T2, '4C3A58D0-1CEB-4CDA-A62D-3B7A72F3B274', 1.000, 1200.00),
                        (NEWID(), @T2, 'EA8D8743-D3D2-4E22-A8A7-97524FACE885', 4.000, 50.00),
                        (NEWID(), @T2, 'A6EF7092-DAB1-4E73-A603-27001BD6D85D', 2.000, 30.00),
                        (NEWID(), @T2, '62B73E75-129E-4489-9AD8-6C8C96FCB527', 2.000, 50.00);
                END

                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[QuickOrderTemplates] WHERE [Name] = 'Special Lunch Deal')
                BEGIN
                    DECLARE @T3 UNIQUEIDENTIFIER = NEWID();
                    INSERT INTO [Restaurant].[QuickOrderTemplates] ([Id], [Name], [Description], [IsActive], [DisplayOrder], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (@T3, 'Special Lunch Deal', N'1 Chicken Haleem Full Plate + 2 Garlic Nan + 1 Fresh Salad', 1, 3, SYSUTCDATETIME(), SYSUTCDATETIME());

                    INSERT INTO [Restaurant].[QuickOrderTemplateItems] ([Id], [TemplateId], [VariantId], [Quantity], [TemplateUnitPrice])
                    VALUES 
                        (NEWID(), @T3, 'A97F1037-DD24-4D63-B8FB-CEC4E5E5C07A', 1.000, 420.00),
                        (NEWID(), @T3, 'EA8D8743-D3D2-4E22-A8A7-97524FACE885', 2.000, 50.00),
                        (NEWID(), @T3, 'A6EF7092-DAB1-4E73-A603-27001BD6D85D', 1.000, 30.00);
                END

                IF NOT EXISTS (SELECT 1 FROM [Restaurant].[QuickOrderTemplates] WHERE [Name] = 'Desi Breakfast Combo')
                BEGIN
                    DECLARE @T4 UNIQUEIDENTIFIER = NEWID();
                    INSERT INTO [Restaurant].[QuickOrderTemplates] ([Id], [Name], [Description], [IsActive], [DisplayOrder], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (@T4, 'Desi Breakfast Combo', N'1 Murgh Chanay Full Plate + 2 Garlic Nan + 1 Cold Beverage', 1, 4, SYSUTCDATETIME(), SYSUTCDATETIME());

                    INSERT INTO [Restaurant].[QuickOrderTemplateItems] ([Id], [TemplateId], [VariantId], [Quantity], [TemplateUnitPrice])
                    VALUES 
                        (NEWID(), @T4, '58CD5A59-4E84-49E4-9C37-5F6DF7939A2C', 1.000, 400.00),
                        (NEWID(), @T4, 'EA8D8743-D3D2-4E22-A8A7-97524FACE885', 2.000, 50.00),
                        (NEWID(), @T4, '62B73E75-129E-4489-9AD8-6C8C96FCB527', 1.000, 50.00);
                END
            END
            """;
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);

        const string tableUniquenessSql = """
            IF EXISTS (
                SELECT [DiningAreaId], [Code]
                FROM [Restaurant].[Tables]
                GROUP BY [DiningAreaId], [Code]
                HAVING COUNT(*) > 1
            )
            BEGIN
                ;WITH Duplicates AS (
                    SELECT [Id], [DiningAreaId], [Code],
                           ROW_NUMBER() OVER (PARTITION BY [DiningAreaId], [Code] ORDER BY [CreatedAtUtc] ASC) AS rn,
                           FIRST_VALUE([Id]) OVER (PARTITION BY [DiningAreaId], [Code] ORDER BY [CreatedAtUtc] ASC) AS KeepId
                    FROM [Restaurant].[Tables]
                )
                UPDATE o
                SET o.[TableId] = d.KeepId
                FROM [Restaurant].[Orders] o
                INNER JOIN Duplicates d ON o.[TableId] = d.[Id]
                WHERE d.rn > 1;

                ;WITH Duplicates AS (
                    SELECT [Id],
                           ROW_NUMBER() OVER (PARTITION BY [DiningAreaId], [Code] ORDER BY [CreatedAtUtc] ASC) AS rn
                    FROM [Restaurant].[Tables]
                )
                DELETE FROM [Restaurant].[Tables]
                WHERE [Id] IN (SELECT [Id] FROM Duplicates WHERE rn > 1);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tables_DiningAreaId_Code' AND object_id = OBJECT_ID(N'[Restaurant].[Tables]'))
            BEGIN
                CREATE UNIQUE NONCLUSTERED INDEX [IX_Tables_DiningAreaId_Code]
                ON [Restaurant].[Tables] ([DiningAreaId], [Code]);
            END
            """;
        await dbContext.Database.ExecuteSqlRawAsync(tableUniquenessSql, cancellationToken);

        const string dayCloseAndShiftConcurrencySql = """
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

                CREATE UNIQUE NONCLUSTERED INDEX [IX_BusinessDayCloses_BranchId_BusinessDate]
                ON [Restaurant].[BusinessDayCloses] ([BranchId], [BusinessDate]);
            END

            -- Enforce database-level concurrency protection for active shifts (Phase 9)
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shifts_TerminalId_Active' AND object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
            BEGIN
                CREATE UNIQUE NONCLUSTERED INDEX [IX_Shifts_TerminalId_Active]
                ON [Restaurant].[Shifts] ([TerminalId])
                WHERE [Status] = 'Open';
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shifts_CashierId_Active' AND object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
            BEGIN
                CREATE UNIQUE NONCLUSTERED INDEX [IX_Shifts_CashierId_Active]
                ON [Restaurant].[Shifts] ([CashierId])
                WHERE [Status] = 'Open';
            END
            """;
        await dbContext.Database.ExecuteSqlRawAsync(dayCloseAndShiftConcurrencySql, cancellationToken);

        if (dbContext.Database.IsSqlServer())
        {
            const string terminalSelfHealSql = """
                SET QUOTED_IDENTIFIER ON;
                IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[Shifts]'))
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'Clovent_MasterData')
                    BEGIN
                        DECLARE @DefaultTerminalId uniqueidentifier = NULL;
                        SELECT TOP 1 @DefaultTerminalId = [Id] 
                        FROM [Clovent_MasterData].[MasterData].[Terminals] 
                        WHERE [Status] = 'Active' 
                        ORDER BY [Code];

                        IF @DefaultTerminalId IS NOT NULL
                        BEGIN
                            UPDATE [Restaurant].[Shifts]
                            SET [TerminalId] = @DefaultTerminalId
                            WHERE [TerminalId] = '00000000-0000-0000-0000-000000000001'
                               OR [TerminalId] = '00000000-0000-0000-0000-000000000000';
                        END
                    END
                END
                """;
            await dbContext.Database.ExecuteSqlRawAsync(terminalSelfHealSql, cancellationToken);
        }
    }
}
