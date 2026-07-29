using Clovent.Catalog.Products;
using Clovent.Catalog.Shared.ValueObjects;

namespace Clovent.Catalog.Application.Tests.TestSupport;

internal sealed class FakeProductRepository : IProductRepository
{
    private readonly Dictionary<ProductId, Product> _products = [];

    public void Add(Product product) => _products[product.Id] = product;

    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.GetValueOrDefault(id));

    public Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.Values.FirstOrDefault(p => p.Sku == sku));

    public Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Product>>([.. _products.Values]);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products[product.Id] = product;
        return Task.CompletedTask;
    }
}
