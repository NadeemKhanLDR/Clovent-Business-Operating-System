using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.WarehouseStocks;

/// <summary>Persistence contract for <see cref="WarehouseStock"/> aggregates.</summary>
public interface IWarehouseStockRepository
{
    /// <summary>Retrieves a stock balance by identity, or <see langword="null"/> if none exists.</summary>
    Task<WarehouseStock?> GetByIdAsync(WarehouseStockId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the stock balance for a specific warehouse/variant pair, or <see langword="null"/> if none exists yet.</summary>
    Task<WarehouseStock?> GetByWarehouseAndVariantAsync(WarehouseId warehouseId, ProductVariantId productVariantId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every stock balance at a warehouse.</summary>
    Task<IReadOnlyCollection<WarehouseStock>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every stock balance across every warehouse.</summary>
    Task<IReadOnlyCollection<WarehouseStock>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created stock balance.</summary>
    Task AddAsync(WarehouseStock stock, CancellationToken cancellationToken = default);
}
