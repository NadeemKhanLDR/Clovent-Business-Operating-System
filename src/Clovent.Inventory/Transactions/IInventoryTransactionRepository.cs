using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Transactions;

/// <summary>Persistence contract for <see cref="InventoryTransaction"/> ledger entries.</summary>
public interface IInventoryTransactionRepository
{
    /// <summary>Retrieves a transaction by identity, or <see langword="null"/> if none exists.</summary>
    Task<InventoryTransaction?> GetByIdAsync(InventoryTransactionId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every transaction recorded at a warehouse.</summary>
    Task<IReadOnlyCollection<InventoryTransaction>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the most recent transactions across every warehouse, newest first.</summary>
    Task<IReadOnlyCollection<InventoryTransaction>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-recorded transaction.</summary>
    Task AddAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default);
}
