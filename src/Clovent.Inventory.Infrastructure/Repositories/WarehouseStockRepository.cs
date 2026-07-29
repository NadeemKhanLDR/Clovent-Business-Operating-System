using Clovent.Catalog.Variants;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Inventory.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IWarehouseStockRepository"/>.</summary>
public sealed class WarehouseStockRepository(InventoryDbContext dbContext) : IWarehouseStockRepository
{
    /// <inheritdoc/>
    public Task<WarehouseStock?> GetByIdAsync(WarehouseStockId id, CancellationToken cancellationToken = default) =>
        dbContext.WarehouseStocks.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<WarehouseStock?> GetByWarehouseAndVariantAsync(WarehouseId warehouseId, ProductVariantId productVariantId, CancellationToken cancellationToken = default) =>
        dbContext.WarehouseStocks.FirstOrDefaultAsync(s => s.WarehouseId == warehouseId && s.ProductVariantId == productVariantId, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<WarehouseStock>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default) =>
        await dbContext.WarehouseStocks.Where(s => s.WarehouseId == warehouseId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<WarehouseStock>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.WarehouseStocks.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(WarehouseStock stock, CancellationToken cancellationToken = default) =>
        await dbContext.WarehouseStocks.AddAsync(stock, cancellationToken);
}
