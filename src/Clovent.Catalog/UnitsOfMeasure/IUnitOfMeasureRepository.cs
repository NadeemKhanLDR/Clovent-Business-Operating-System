using Clovent.Catalog.UnitsOfMeasure.ValueObjects;

namespace Clovent.Catalog.UnitsOfMeasure;

/// <summary>Persistence contract for <see cref="UnitOfMeasure"/> aggregates.</summary>
public interface IUnitOfMeasureRepository
{
    /// <summary>Retrieves a unit by identity, or <see langword="null"/> if none exists.</summary>
    Task<UnitOfMeasure?> GetByIdAsync(UnitOfMeasureId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a unit by its code, or <see langword="null"/> if none exists.</summary>
    Task<UnitOfMeasure?> GetByCodeAsync(UnitOfMeasureCode code, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every unit in the catalog.</summary>
    Task<IReadOnlyCollection<UnitOfMeasure>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created unit.</summary>
    Task AddAsync(UnitOfMeasure unit, CancellationToken cancellationToken = default);
}
