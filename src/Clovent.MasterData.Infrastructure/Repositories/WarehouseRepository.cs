using Clovent.Identity.Branches;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.MasterData.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IWarehouseRepository"/>.</summary>
public sealed class WarehouseRepository(MasterDataDbContext dbContext) : IWarehouseRepository
{
    /// <inheritdoc/>
    public async Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken cancellationToken = default) =>
        await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Warehouse>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
        await dbContext.Warehouses.Where(w => w.BranchId == branchId).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Warehouses.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default) =>
        await dbContext.Warehouses.AddAsync(warehouse, cancellationToken).ConfigureAwait(false);
}
