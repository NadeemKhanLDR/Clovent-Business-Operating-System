using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Tables;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeTableRepository : ITableRepository
{
    private readonly Dictionary<TableId, Table> _tables = [];

    public void Add(Table table) => _tables[table.Id] = table;

    public Task<Table?> GetByIdAsync(TableId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tables.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Table>> GetByDiningAreaIdAsync(DiningAreaId diningAreaId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Table>>([.. _tables.Values.Where(t => t.DiningAreaId == diningAreaId)]);

    public Task<IReadOnlyCollection<Table>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Table>>([.. _tables.Values]);

    public Task AddAsync(Table table, CancellationToken cancellationToken = default)
    {
        _tables[table.Id] = table;
        return Task.CompletedTask;
    }
}
