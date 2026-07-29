using Clovent.Identity.Branches;
using Clovent.Restaurant.DiningAreas;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeDiningAreaRepository : IDiningAreaRepository
{
    private readonly Dictionary<DiningAreaId, DiningArea> _areas = [];

    public void Add(DiningArea area) => _areas[area.Id] = area;

    public Task<DiningArea?> GetByIdAsync(DiningAreaId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_areas.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<DiningArea>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<DiningArea>>([.. _areas.Values.Where(a => a.BranchId == branchId)]);

    public Task<IReadOnlyCollection<DiningArea>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<DiningArea>>([.. _areas.Values]);

    public Task AddAsync(DiningArea diningArea, CancellationToken cancellationToken = default)
    {
        _areas[diningArea.Id] = diningArea;
        return Task.CompletedTask;
    }
}
