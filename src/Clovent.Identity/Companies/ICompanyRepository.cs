using Clovent.Identity.Organizations;

namespace Clovent.Identity.Companies;

/// <summary>
/// Persistence contract for <see cref="Company"/> aggregates. No
/// implementation exists yet - this is the seam a future
/// Infrastructure/Persistence milestone implements against.
/// </summary>
public interface ICompanyRepository
{
    /// <summary>Retrieves a company by identity, or <see langword="null"/> if none exists.</summary>
    Task<Company?> GetByIdAsync(CompanyId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every company belonging to the given organization.</summary>
    Task<IReadOnlyCollection<Company>> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created company.</summary>
    Task AddAsync(Company company, CancellationToken cancellationToken = default);
}
