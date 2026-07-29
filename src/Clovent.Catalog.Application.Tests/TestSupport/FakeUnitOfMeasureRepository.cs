using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.UnitsOfMeasure.ValueObjects;

namespace Clovent.Catalog.Application.Tests.TestSupport;

internal sealed class FakeUnitOfMeasureRepository : IUnitOfMeasureRepository
{
    private readonly Dictionary<UnitOfMeasureId, UnitOfMeasure> _units = [];

    public void Add(UnitOfMeasure unit) => _units[unit.Id] = unit;

    public Task<UnitOfMeasure?> GetByIdAsync(UnitOfMeasureId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_units.GetValueOrDefault(id));

    public Task<UnitOfMeasure?> GetByCodeAsync(UnitOfMeasureCode code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_units.Values.FirstOrDefault(u => u.Code == code));

    public Task<IReadOnlyCollection<UnitOfMeasure>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<UnitOfMeasure>>([.. _units.Values]);

    public Task AddAsync(UnitOfMeasure unit, CancellationToken cancellationToken = default)
    {
        _units[unit.Id] = unit;
        return Task.CompletedTask;
    }
}
