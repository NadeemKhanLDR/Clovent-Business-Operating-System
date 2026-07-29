using Clovent.Catalog.Categories;
using Clovent.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IProductCategoryRepository"/>.</summary>
public sealed class ProductCategoryRepository(CatalogDbContext dbContext) : IProductCategoryRepository
{
    /// <inheritdoc/>
    public Task<ProductCategory?> GetByIdAsync(ProductCategoryId id, CancellationToken cancellationToken = default) =>
        dbContext.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ProductCategories.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default) =>
        await dbContext.ProductCategories.AddAsync(category, cancellationToken);
}
