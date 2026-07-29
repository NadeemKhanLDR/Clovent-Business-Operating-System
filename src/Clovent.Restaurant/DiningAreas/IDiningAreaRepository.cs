using Clovent.Identity.Branches;

namespace Clovent.Restaurant.DiningAreas;

/// <summary>Persistence contract for <see cref="DiningArea"/> aggregates.</summary>
public interface IDiningAreaRepository
{
    /// <summary>Retrieves a dining area by identity, or <see langword="null"/> if none exists.</summary>
    Task<DiningArea?> GetByIdAsync(DiningAreaId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every dining area belonging to a branch.</summary>
    Task<IReadOnlyCollection<DiningArea>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every dining area across every branch - used to build the Table Management scoping picker.</summary>
    Task<IReadOnlyCollection<DiningArea>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created dining area.</summary>
    Task AddAsync(DiningArea diningArea, CancellationToken cancellationToken = default);
}
