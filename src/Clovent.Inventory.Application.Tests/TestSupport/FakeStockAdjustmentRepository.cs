using Clovent.Inventory.Adjustments;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Application.Tests.TestSupport;

internal sealed class FakeStockAdjustmentRepository : IStockAdjustmentRepository
{
    private readonly Dictionary<StockAdjustmentId, StockAdjustment> _adjustments = [];

    public void Add(StockAdjustment adjustment) => _adjustments[adjustment.Id] = adjustment;

    public Task<StockAdjustment?> GetByIdAsync(StockAdjustmentId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_adjustments.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<StockAdjustment>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<StockAdjustment>>([.. _adjustments.Values.Where(a => a.WarehouseId == warehouseId)]);

    public Task<IReadOnlyCollection<StockAdjustment>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<StockAdjustment>>([.. _adjustments.Values]);

    public Task AddAsync(StockAdjustment adjustment, CancellationToken cancellationToken = default)
    {
        _adjustments[adjustment.Id] = adjustment;
        return Task.CompletedTask;
    }
}
