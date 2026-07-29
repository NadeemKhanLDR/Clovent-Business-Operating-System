using Clovent.Catalog.Products;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.Variants;

namespace Clovent.Catalog.Application.Tests.TestSupport;

internal sealed class FakeProductVariantRepository : IProductVariantRepository
{
    private readonly Dictionary<ProductVariantId, ProductVariant> _variants = [];

    public void Add(ProductVariant variant) => _variants[variant.Id] = variant;

    public Task<ProductVariant?> GetByIdAsync(ProductVariantId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_variants.GetValueOrDefault(id));

    public Task<ProductVariant?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_variants.Values.FirstOrDefault(v => v.Sku == sku));

    public Task<IReadOnlyCollection<ProductVariant>> GetByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ProductVariant>>([.. _variants.Values.Where(v => v.ProductId == productId)]);

    public Task<IReadOnlyCollection<ProductVariant>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ProductVariant>>([.. _variants.Values]);

    public Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        _variants[variant.Id] = variant;
        return Task.CompletedTask;
    }
}
