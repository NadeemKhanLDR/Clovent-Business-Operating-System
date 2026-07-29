using Clovent.Catalog.Variants;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Application.Tests.TestSupport;

internal sealed class FakeWarehouseStockRepository : IWarehouseStockRepository
{
    private readonly Dictionary<WarehouseStockId, WarehouseStock> _stocks = [];

    public void Add(WarehouseStock stock) => _stocks[stock.Id] = stock;

    public Task<WarehouseStock?> GetByIdAsync(WarehouseStockId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stocks.GetValueOrDefault(id));

    public Task<WarehouseStock?> GetByWarehouseAndVariantAsync(WarehouseId warehouseId, ProductVariantId productVariantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stocks.Values.FirstOrDefault(s => s.WarehouseId == warehouseId && s.ProductVariantId == productVariantId));

    public Task<IReadOnlyCollection<WarehouseStock>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<WarehouseStock>>([.. _stocks.Values.Where(s => s.WarehouseId == warehouseId)]);

    public Task<IReadOnlyCollection<WarehouseStock>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<WarehouseStock>>([.. _stocks.Values]);

    public Task AddAsync(WarehouseStock stock, CancellationToken cancellationToken = default)
    {
        _stocks[stock.Id] = stock;
        return Task.CompletedTask;
    }
}
