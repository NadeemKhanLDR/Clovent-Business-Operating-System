using Clovent.Identity.Branches;
using Clovent.MasterData.Warehouses;

namespace Clovent.MasterData.Application.Tests.TestSupport;

internal sealed class FakeWarehouseRepository : IWarehouseRepository
{
    private readonly Dictionary<WarehouseId, Warehouse> _warehouses = [];

    public void Add(Warehouse warehouse) => _warehouses[warehouse.Id] = warehouse;

    public Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_warehouses.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Warehouse>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Warehouse>>([.. _warehouses.Values.Where(w => w.BranchId == branchId)]);

    public Task<IReadOnlyCollection<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Warehouse>>([.. _warehouses.Values]);

    public Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        _warehouses[warehouse.Id] = warehouse;
        return Task.CompletedTask;
    }
}
