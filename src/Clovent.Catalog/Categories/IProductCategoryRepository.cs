namespace Clovent.Catalog.Categories;

/// <summary>Persistence contract for <see cref="ProductCategory"/> aggregates.</summary>
public interface IProductCategoryRepository
{
    /// <summary>Retrieves a category by identity, or <see langword="null"/> if none exists.</summary>
    Task<ProductCategory?> GetByIdAsync(ProductCategoryId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every category in the catalog.</summary>
    Task<IReadOnlyCollection<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created category.</summary>
    Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default);
}
