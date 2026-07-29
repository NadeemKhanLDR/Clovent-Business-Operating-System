using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.Transfers;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Inventory.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IStockTransferRepository"/>.</summary>
public sealed class StockTransferRepository(InventoryDbContext dbContext) : IStockTransferRepository
{
    /// <inheritdoc/>
    public Task<StockTransfer?> GetByIdAsync(StockTransferId id, CancellationToken cancellationToken = default) =>
        dbContext.StockTransfers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<StockTransfer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.StockTransfers.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default) =>
        await dbContext.StockTransfers.AddAsync(transfer, cancellationToken);
}
