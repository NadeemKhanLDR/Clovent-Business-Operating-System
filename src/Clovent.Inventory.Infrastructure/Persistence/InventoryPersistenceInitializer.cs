using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Inventory.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations for <see cref="InventoryDbContext"/> at startup.</summary>
public sealed class InventoryPersistenceInitializer(InventoryDbContext dbContext) : IPersistenceInitializer
{
    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
