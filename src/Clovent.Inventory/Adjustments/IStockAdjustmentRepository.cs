using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Adjustments;

/// <summary>Persistence contract for <see cref="StockAdjustment"/> aggregates.</summary>
public interface IStockAdjustmentRepository
{
    /// <summary>Retrieves an adjustment by identity, or <see langword="null"/> if none exists.</summary>
    Task<StockAdjustment?> GetByIdAsync(StockAdjustmentId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every adjustment proposed for a warehouse.</summary>
    Task<IReadOnlyCollection<StockAdjustment>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every adjustment.</summary>
    Task<IReadOnlyCollection<StockAdjustment>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-proposed adjustment.</summary>
    Task AddAsync(StockAdjustment adjustment, CancellationToken cancellationToken = default);
}
