using Clovent.Catalog.Shared.ValueObjects;

namespace Clovent.Catalog.Products;

/// <summary>Persistence contract for <see cref="Product"/> aggregates.</summary>
public interface IProductRepository
{
    /// <summary>Retrieves a product by identity, or <see langword="null"/> if none exists.</summary>
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a product by its SKU, or <see langword="null"/> if none exists.</summary>
    Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every product in the catalog.</summary>
    Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created product.</summary>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}
