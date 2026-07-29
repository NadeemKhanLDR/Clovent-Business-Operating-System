using Clovent.MasterData.Warehouses;

namespace Clovent.Restaurant.Sales;

/// <summary>Persistence contract for <see cref="DailySalesSequence"/> aggregates.</summary>
public interface IDailySalesSequenceRepository
{
    /// <summary>Retrieves the sequence for a warehouse/date pair, or <see langword="null"/> if none exists yet.</summary>
    Task<DailySalesSequence?> GetByWarehouseAndDateAsync(WarehouseId warehouseId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created sequence.</summary>
    Task AddAsync(DailySalesSequence sequence, CancellationToken cancellationToken = default);
}
