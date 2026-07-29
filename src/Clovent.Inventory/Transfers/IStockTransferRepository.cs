namespace Clovent.Inventory.Transfers;

/// <summary>Persistence contract for <see cref="StockTransfer"/> aggregates.</summary>
public interface IStockTransferRepository
{
    /// <summary>Retrieves a transfer by identity, or <see langword="null"/> if none exists.</summary>
    Task<StockTransfer?> GetByIdAsync(StockTransferId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every transfer.</summary>
    Task<IReadOnlyCollection<StockTransfer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-proposed transfer.</summary>
    Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default);
}
