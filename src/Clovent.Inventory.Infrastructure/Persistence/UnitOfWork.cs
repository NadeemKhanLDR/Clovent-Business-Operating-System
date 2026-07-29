using Clovent.Inventory.Application;

namespace Clovent.Inventory.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>, wrapping <see cref="InventoryDbContext"/>'s <c>SaveChangesAsync</c>. Mirrors <c>Clovent.Catalog.Infrastructure.Persistence.UnitOfWork</c>.</summary>
public sealed class UnitOfWork(InventoryDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
