using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.Products;
using Clovent.Catalog.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IProductRepository"/>.</summary>
public sealed class ProductRepository(CatalogDbContext dbContext) : IProductRepository
{
    /// <inheritdoc/>
    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        await dbContext.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Products.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await dbContext.Products.AddAsync(product, cancellationToken).ConfigureAwait(false);
}
