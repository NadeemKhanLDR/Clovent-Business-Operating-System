using Clovent.Identity.Companies;

namespace Clovent.Identity.Branches;

/// <summary>
/// Persistence contract for <see cref="Branch"/> aggregates. No
/// implementation exists yet - this is the seam a future
/// Infrastructure/Persistence milestone implements against.
/// </summary>
public interface IBranchRepository
{
    /// <summary>Retrieves a branch by identity, or <see langword="null"/> if none exists.</summary>
    Task<Branch?> GetByIdAsync(BranchId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every branch belonging to the given company.</summary>
    Task<IReadOnlyCollection<Branch>> GetByCompanyIdAsync(CompanyId companyId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created branch.</summary>
    Task AddAsync(Branch branch, CancellationToken cancellationToken = default);
}
