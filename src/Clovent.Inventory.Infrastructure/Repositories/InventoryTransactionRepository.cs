using Clovent.Catalog.Variants;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.Transactions;
using Clovent.MasterData.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Inventory.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IInventoryTransactionRepository"/>.</summary>
public sealed class InventoryTransactionRepository(InventoryDbContext dbContext) : IInventoryTransactionRepository
{
    /// <inheritdoc/>
    public Task<InventoryTransaction?> GetByIdAsync(InventoryTransactionId id, CancellationToken cancellationToken = default) =>
        dbContext.InventoryTransactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventoryTransaction>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default) =>
        await dbContext.InventoryTransactions.Where(t => t.WarehouseId == warehouseId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventoryTransaction>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default) =>
        await dbContext.InventoryTransactions.Where(t => t.ProductVariantId == productVariantId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventoryTransaction>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        await dbContext.InventoryTransactions.OrderByDescending(t => t.OccurredAtUtc).Take(count).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventoryTransaction>> GetByReferenceAsync(string referenceType, Guid referenceId, CancellationToken cancellationToken = default) =>
        await dbContext.InventoryTransactions
            .Where(t => t.ReferenceType == referenceType && t.ReferenceId == referenceId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default) =>
        await dbContext.InventoryTransactions.AddAsync(transaction, cancellationToken);
}
