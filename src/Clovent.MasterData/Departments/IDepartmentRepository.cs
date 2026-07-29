using Clovent.Identity.Branches;

namespace Clovent.MasterData.Departments;

/// <summary>Persistence contract for <see cref="Department"/> aggregates.</summary>
public interface IDepartmentRepository
{
    /// <summary>Retrieves a department by identity, or <see langword="null"/> if none exists.</summary>
    Task<Department?> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every department belonging to a branch.</summary>
    Task<IReadOnlyCollection<Department>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created department.</summary>
    Task AddAsync(Department department, CancellationToken cancellationToken = default);
}
