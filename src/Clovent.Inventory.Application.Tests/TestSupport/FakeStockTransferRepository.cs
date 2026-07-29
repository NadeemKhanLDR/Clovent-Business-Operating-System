using Clovent.Inventory.Transfers;

namespace Clovent.Inventory.Application.Tests.TestSupport;

internal sealed class FakeStockTransferRepository : IStockTransferRepository
{
    private readonly Dictionary<StockTransferId, StockTransfer> _transfers = [];

    public void Add(StockTransfer transfer) => _transfers[transfer.Id] = transfer;

    public Task<StockTransfer?> GetByIdAsync(StockTransferId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_transfers.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<StockTransfer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<StockTransfer>>([.. _transfers.Values]);

    public Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default)
    {
        _transfers[transfer.Id] = transfer;
        return Task.CompletedTask;
    }
}
