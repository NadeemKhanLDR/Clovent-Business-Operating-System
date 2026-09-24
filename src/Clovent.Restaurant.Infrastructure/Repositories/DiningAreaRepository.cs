using Clovent.Identity.Branches;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IDiningAreaRepository"/>.</summary>
public sealed class DiningAreaRepository(RestaurantDbContext dbContext) : IDiningAreaRepository
{
    /// <inheritdoc/>
    public async Task<DiningArea?> GetByIdAsync(DiningAreaId id, CancellationToken cancellationToken = default) =>
        await dbContext.DiningAreas.FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<DiningArea>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
        await dbContext.DiningAreas.Where(a => a.BranchId == branchId).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<DiningArea>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.DiningAreas.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(DiningArea diningArea, CancellationToken cancellationToken = default) =>
        await dbContext.DiningAreas.AddAsync(diningArea, cancellationToken).ConfigureAwait(false);
}
