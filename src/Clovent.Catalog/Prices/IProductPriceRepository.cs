using Clovent.Catalog.Variants;

namespace Clovent.Catalog.Prices;

/// <summary>Persistence contract for <see cref="ProductPrice"/> aggregates.</summary>
public interface IProductPriceRepository
{
    /// <summary>Retrieves a price record by identity, or <see langword="null"/> if none exists.</summary>
    Task<ProductPrice?> GetByIdAsync(ProductPriceId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every price record (cost and selling, active and inactive) for a variant.</summary>
    Task<IReadOnlyCollection<ProductPrice>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves every currently-active price record of the given type,
    /// across every variant, in one call - the flat "list all" query a
    /// screen scoping many variants at once (POS's product tile wall, Menu
    /// Items) needs so it isn't forced into one <see cref="GetByProductVariantIdAsync"/>
    /// call per variant to resolve "what does this cost right now" at
    /// screen-load time. Mirrors the same additive-query pattern
    /// <c>ListAllWarehousesQuery</c>/<c>ListAllTablesQuery</c> already
    /// establish elsewhere in this solution.
    /// </summary>
    Task<IReadOnlyCollection<ProductPrice>> GetActiveByPriceTypeAsync(PriceType priceType, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created price record.</summary>
    Task AddAsync(ProductPrice price, CancellationToken cancellationToken = default);
}
