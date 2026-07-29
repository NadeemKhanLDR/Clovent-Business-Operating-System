using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.MasterData.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Inventory.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IStockAdjustmentRepository"/>.</summary>
public sealed class StockAdjustmentRepository(InventoryDbContext dbContext) : IStockAdjustmentRepository
{
    /// <inheritdoc/>
    public Task<StockAdjustment?> GetByIdAsync(StockAdjustmentId id, CancellationToken cancellationToken = default) =>
        dbContext.StockAdjustments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<StockAdjustment>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default) =>
        await dbContext.StockAdjustments.Where(a => a.WarehouseId == warehouseId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<StockAdjustment>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.StockAdjustments.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(StockAdjustment adjustment, CancellationToken cancellationToken = default) =>
        await dbContext.StockAdjustments.AddAsync(adjustment, cancellationToken);
}
