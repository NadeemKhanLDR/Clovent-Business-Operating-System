using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Sales;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeDailySalesSequenceRepository : IDailySalesSequenceRepository
{
    private readonly Dictionary<(WarehouseId, DateOnly), DailySalesSequence> _sequences = [];

    public void Add(DailySalesSequence sequence) => _sequences[(sequence.WarehouseId, sequence.Date)] = sequence;

    public Task<DailySalesSequence?> GetByWarehouseAndDateAsync(WarehouseId warehouseId, DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sequences.GetValueOrDefault((warehouseId, date)));

    public Task AddAsync(DailySalesSequence sequence, CancellationToken cancellationToken = default)
    {
        _sequences[(sequence.WarehouseId, sequence.Date)] = sequence;
        return Task.CompletedTask;
    }
}
