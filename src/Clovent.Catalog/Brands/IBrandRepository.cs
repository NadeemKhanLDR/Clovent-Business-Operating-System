namespace Clovent.Catalog.Brands;

/// <summary>Persistence contract for <see cref="Brand"/> aggregates.</summary>
public interface IBrandRepository
{
    /// <summary>Retrieves a brand by identity, or <see langword="null"/> if none exists.</summary>
    Task<Brand?> GetByIdAsync(BrandId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every brand in the catalog.</summary>
    Task<IReadOnlyCollection<Brand>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created brand.</summary>
    Task AddAsync(Brand brand, CancellationToken cancellationToken = default);
}
