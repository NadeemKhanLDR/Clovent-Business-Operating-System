using Clovent.Catalog.Prices;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class InMemoryWarehouseStockRepository : IWarehouseStockRepository
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

internal sealed class InMemoryProductPriceRepository : IProductPriceRepository
{
    private readonly Dictionary<ProductPriceId, ProductPrice> _prices = [];

    public void Add(ProductPrice price) => _prices[price.Id] = price;

    public Task<ProductPrice?> GetByIdAsync(ProductPriceId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_prices.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<ProductPrice>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ProductPrice>>([.. _prices.Values.Where(p => p.ProductVariantId == productVariantId)]);

    public Task<IReadOnlyCollection<ProductPrice>> GetActiveByPriceTypeAsync(PriceType priceType, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ProductPrice>>([.. _prices.Values.Where(p => p.PriceType == priceType && p.Status == CatalogStatus.Active)]);

    public Task AddAsync(ProductPrice price, CancellationToken cancellationToken = default)
    {
        _prices[price.Id] = price;
        return Task.CompletedTask;
    }
}
