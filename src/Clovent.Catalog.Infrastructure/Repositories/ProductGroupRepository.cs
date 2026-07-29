using Clovent.Catalog.Groups;
using Clovent.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IProductGroupRepository"/>.</summary>
public sealed class ProductGroupRepository(CatalogDbContext dbContext) : IProductGroupRepository
{
    /// <inheritdoc/>
    public Task<ProductGroup?> GetByIdAsync(ProductGroupId id, CancellationToken cancellationToken = default) =>
        dbContext.ProductGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductGroup>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ProductGroups.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(ProductGroup group, CancellationToken cancellationToken = default) =>
        await dbContext.ProductGroups.AddAsync(group, cancellationToken);
}
