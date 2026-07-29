using Clovent.Catalog.Products;
using Clovent.Catalog.Shared.ValueObjects;

namespace Clovent.Catalog.Variants;

/// <summary>Persistence contract for <see cref="ProductVariant"/> aggregates.</summary>
public interface IProductVariantRepository
{
    /// <summary>Retrieves a variant by identity, or <see langword="null"/> if none exists.</summary>
    Task<ProductVariant?> GetByIdAsync(ProductVariantId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a variant by its SKU, or <see langword="null"/> if none exists.</summary>
    Task<ProductVariant?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every variant belonging to a product.</summary>
    Task<IReadOnlyCollection<ProductVariant>> GetByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every variant in the catalog.</summary>
    Task<IReadOnlyCollection<ProductVariant>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created variant.</summary>
    Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default);
}
