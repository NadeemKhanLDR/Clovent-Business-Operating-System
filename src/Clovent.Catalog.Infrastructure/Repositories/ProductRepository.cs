using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.Products;
using Clovent.Catalog.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IProductRepository"/>.</summary>
public sealed class ProductRepository(CatalogDbContext dbContext) : IProductRepository
{
    /// <inheritdoc/>
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Products.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await dbContext.Products.AddAsync(product, cancellationToken);
}
