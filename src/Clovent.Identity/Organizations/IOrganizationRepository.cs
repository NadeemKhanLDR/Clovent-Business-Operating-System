namespace Clovent.Identity.Organizations;

/// <summary>
/// Persistence contract for <see cref="Organization"/> aggregates. No
/// implementation exists yet - this is the seam a future
/// Infrastructure/Persistence milestone implements against.
/// </summary>
public interface IOrganizationRepository
{
    /// <summary>Retrieves an organization by identity, or <see langword="null"/> if none exists.</summary>
    Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every organization.</summary>
    Task<IReadOnlyCollection<Organization>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created organization.</summary>
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
}
