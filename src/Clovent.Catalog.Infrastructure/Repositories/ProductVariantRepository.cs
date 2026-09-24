using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.Products;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.Variants;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IProductVariantRepository"/>.</summary>
public sealed class ProductVariantRepository(CatalogDbContext dbContext) : IProductVariantRepository
{
    /// <inheritdoc/>
    public async Task<ProductVariant?> GetByIdAsync(ProductVariantId id, CancellationToken cancellationToken = default) =>
        await dbContext.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<ProductVariant?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        await dbContext.ProductVariants.FirstOrDefaultAsync(v => v.Sku == sku, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductVariant>> GetByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default) =>
        await dbContext.ProductVariants.Where(v => v.ProductId == productId).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductVariant>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ProductVariants.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default) =>
        await dbContext.ProductVariants.AddAsync(variant, cancellationToken).ConfigureAwait(false);
}
