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
    public Task<ProductVariant?> GetByIdAsync(ProductVariantId id, CancellationToken cancellationToken = default) =>
        dbContext.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<ProductVariant?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        dbContext.ProductVariants.FirstOrDefaultAsync(v => v.Sku == sku, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductVariant>> GetByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default) =>
        await dbContext.ProductVariants.Where(v => v.ProductId == productId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductVariant>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ProductVariants.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default) =>
        await dbContext.ProductVariants.AddAsync(variant, cancellationToken);
}
