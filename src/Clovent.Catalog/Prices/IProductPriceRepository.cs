using Clovent.Catalog.Variants;

namespace Clovent.Catalog.Prices;

/// <summary>Persistence contract for <see cref="ProductPrice"/> aggregates.</summary>
public interface IProductPriceRepository
{
    /// <summary>Retrieves a price record by identity, or <see langword="null"/> if none exists.</summary>
    Task<ProductPrice?> GetByIdAsync(ProductPriceId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every price record (cost and selling, active and inactive) for a variant.</summary>
    Task<IReadOnlyCollection<ProductPrice>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created price record.</summary>
    Task AddAsync(ProductPrice price, CancellationToken cancellationToken = default);
}
