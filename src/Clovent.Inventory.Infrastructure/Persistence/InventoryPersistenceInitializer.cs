using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Inventory.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations for <see cref="InventoryDbContext"/> at startup.</summary>
public sealed class InventoryPersistenceInitializer(InventoryDbContext dbContext) : IPersistenceInitializer
{
    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (dbContext.Database.IsSqlServer())
        {
            const string schemaSql = """
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
                """;
            await dbContext.Database.ExecuteSqlRawAsync(schemaSql, cancellationToken);
        }
    }
}
