using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Variants;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IProductPriceRepository"/>.</summary>
public sealed class ProductPriceRepository(CatalogDbContext dbContext) : IProductPriceRepository
{
    /// <inheritdoc/>
    public Task<ProductPrice?> GetByIdAsync(ProductPriceId id, CancellationToken cancellationToken = default) =>
        dbContext.ProductPrices.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductPrice>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default) =>
        await dbContext.ProductPrices.Where(p => p.ProductVariantId == productVariantId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductPrice>> GetActiveByPriceTypeAsync(PriceType priceType, CancellationToken cancellationToken = default) =>
        await dbContext.ProductPrices.Where(p => p.PriceType == priceType && p.Status == Clovent.Catalog.Shared.CatalogStatus.Active).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(ProductPrice price, CancellationToken cancellationToken = default) =>
        await dbContext.ProductPrices.AddAsync(price, cancellationToken);
}
