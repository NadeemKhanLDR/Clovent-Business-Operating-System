using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations for <see cref="CatalogDbContext"/> at startup.</summary>
public sealed class CatalogPersistenceInitializer(CatalogDbContext dbContext) : IPersistenceInitializer
{
    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        const string sql = """
            IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Catalog].[Products]'))
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[Catalog].[Products]') 
                    AND name = 'ItemType'
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
                    AND name = 'IsAvailable'
                )
                BEGIN
                    ALTER TABLE [Catalog].[ProductVariants] ADD [IsAvailable] bit NOT NULL CONSTRAINT [DF_ProductVariants_IsAvailable] DEFAULT 1;
                END
            END
            """;
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
