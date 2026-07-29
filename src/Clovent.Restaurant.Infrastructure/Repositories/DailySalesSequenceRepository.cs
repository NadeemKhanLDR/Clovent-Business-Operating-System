using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Sales;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IDailySalesSequenceRepository"/>.</summary>
public sealed class DailySalesSequenceRepository(RestaurantDbContext dbContext) : IDailySalesSequenceRepository
{
    /// <inheritdoc/>
    public Task<DailySalesSequence?> GetByWarehouseAndDateAsync(WarehouseId warehouseId, DateOnly date, CancellationToken cancellationToken = default) =>
        dbContext.DailySalesSequences.FirstOrDefaultAsync(s => s.WarehouseId == warehouseId && s.Date == date, cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(DailySalesSequence sequence, CancellationToken cancellationToken = default) =>
        await dbContext.DailySalesSequences.AddAsync(sequence, cancellationToken);
}
