using Clovent.Catalog.Brands;
using Clovent.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IBrandRepository"/>.</summary>
public sealed class BrandRepository(CatalogDbContext dbContext) : IBrandRepository
{
    /// <inheritdoc/>
    public Task<Brand?> GetByIdAsync(BrandId id, CancellationToken cancellationToken = default) =>
        dbContext.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Brand>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Brands.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Brand brand, CancellationToken cancellationToken = default) =>
        await dbContext.Brands.AddAsync(brand, cancellationToken);
}
