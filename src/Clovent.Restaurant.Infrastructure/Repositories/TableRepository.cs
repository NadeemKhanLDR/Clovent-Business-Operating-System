using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Tables;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ITableRepository"/>.</summary>
public sealed class TableRepository(RestaurantDbContext dbContext) : ITableRepository
{
    /// <inheritdoc/>
    public Task<Table?> GetByIdAsync(TableId id, CancellationToken cancellationToken = default) =>
        dbContext.Tables.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Table>> GetByDiningAreaIdAsync(DiningAreaId diningAreaId, CancellationToken cancellationToken = default) =>
        await dbContext.Tables.Where(t => t.DiningAreaId == diningAreaId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Table>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Tables.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Table table, CancellationToken cancellationToken = default) =>
        await dbContext.Tables.AddAsync(table, cancellationToken);
}
