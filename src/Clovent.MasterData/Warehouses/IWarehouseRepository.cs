using Clovent.Identity.Branches;

namespace Clovent.MasterData.Warehouses;

/// <summary>Persistence contract for <see cref="Warehouse"/> aggregates.</summary>
public interface IWarehouseRepository
{
    /// <summary>Retrieves a warehouse by identity, or <see langword="null"/> if none exists.</summary>
    Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every warehouse belonging to a branch.</summary>
    Task<IReadOnlyCollection<Warehouse>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every warehouse across every branch - used by Milestone 14 ("Product Catalog &amp; Inventory Foundation") Inventory screens, which scope by warehouse directly rather than drilling down the Organization/Company/Branch hierarchy.</summary>
    Task<IReadOnlyCollection<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created warehouse.</summary>
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
}
