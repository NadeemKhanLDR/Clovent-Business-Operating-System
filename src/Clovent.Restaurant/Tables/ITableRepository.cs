using Clovent.Restaurant.DiningAreas;

namespace Clovent.Restaurant.Tables;

/// <summary>Persistence contract for <see cref="Table"/> aggregates.</summary>
public interface ITableRepository
{
    /// <summary>Retrieves a table by identity, or <see langword="null"/> if none exists.</summary>
    Task<Table?> GetByIdAsync(TableId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every table belonging to a dining area.</summary>
    Task<IReadOnlyCollection<Table>> GetByDiningAreaIdAsync(DiningAreaId diningAreaId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every table across every dining area - used to build the floor-plan/POS table grid.</summary>
    Task<IReadOnlyCollection<Table>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created table.</summary>
    Task AddAsync(Table table, CancellationToken cancellationToken = default);
}
