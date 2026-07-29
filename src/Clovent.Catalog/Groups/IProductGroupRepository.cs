namespace Clovent.Catalog.Groups;

/// <summary>Persistence contract for <see cref="ProductGroup"/> aggregates.</summary>
public interface IProductGroupRepository
{
    /// <summary>Retrieves a group by identity, or <see langword="null"/> if none exists.</summary>
    Task<ProductGroup?> GetByIdAsync(ProductGroupId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every group in the catalog.</summary>
    Task<IReadOnlyCollection<ProductGroup>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created group.</summary>
    Task AddAsync(ProductGroup group, CancellationToken cancellationToken = default);
}
