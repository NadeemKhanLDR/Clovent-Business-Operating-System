using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Transactions;

/// <summary>Persistence contract for <see cref="InventoryTransaction"/> ledger entries.</summary>
public interface IInventoryTransactionRepository
{
    /// <summary>Retrieves a transaction by identity, or <see langword="null"/> if none exists.</summary>
    Task<InventoryTransaction?> GetByIdAsync(InventoryTransactionId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every transaction recorded at a warehouse.</summary>
    Task<IReadOnlyCollection<InventoryTransaction>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every transaction recorded for a product variant, across every warehouse - the "Stock History" feature's per-product view.</summary>
    Task<IReadOnlyCollection<InventoryTransaction>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the most recent transactions across every warehouse, newest first.</summary>
    Task<IReadOnlyCollection<InventoryTransaction>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves every transaction recorded against one originating document -
    /// e.g. <c>("Order", orderId)</c> for the stock a restaurant order issued
    /// when it completed. Lets a caller ask "has this document already moved
    /// stock?" before moving it again, which is what makes a retried
    /// completion idempotent rather than a second depletion.
    /// </summary>
    Task<IReadOnlyCollection<InventoryTransaction>> GetByReferenceAsync(string referenceType, Guid referenceId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-recorded transaction.</summary>
    Task AddAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default);
}
