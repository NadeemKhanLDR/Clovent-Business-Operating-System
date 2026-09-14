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
            """;
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
