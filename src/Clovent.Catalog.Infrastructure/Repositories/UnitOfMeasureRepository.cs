using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.UnitsOfMeasure.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IUnitOfMeasureRepository"/>.</summary>
public sealed class UnitOfMeasureRepository(CatalogDbContext dbContext) : IUnitOfMeasureRepository
{
    /// <inheritdoc/>
    public Task<UnitOfMeasure?> GetByIdAsync(UnitOfMeasureId id, CancellationToken cancellationToken = default) =>
        dbContext.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<UnitOfMeasure?> GetByCodeAsync(UnitOfMeasureCode code, CancellationToken cancellationToken = default) =>
        dbContext.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Code == code, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<UnitOfMeasure>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.UnitsOfMeasure.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(UnitOfMeasure unit, CancellationToken cancellationToken = default) =>
        await dbContext.UnitsOfMeasure.AddAsync(unit, cancellationToken);
}
