using Clovent.Catalog.Prices;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;

namespace Clovent.Catalog.Application.Tests.TestSupport;

internal sealed class FakeProductPriceRepository : IProductPriceRepository
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
