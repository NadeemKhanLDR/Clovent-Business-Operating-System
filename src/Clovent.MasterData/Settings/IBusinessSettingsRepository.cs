using Clovent.Identity.Organizations;

namespace Clovent.MasterData.Settings;

/// <summary>Persistence contract for <see cref="BusinessSettings"/> aggregates.</summary>
public interface IBusinessSettingsRepository
{
    /// <summary>Retrieves a business settings record by identity, or <see langword="null"/> if none exists.</summary>
    Task<BusinessSettings?> GetByIdAsync(BusinessSettingsId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the business settings record for an organization, or <see langword="null"/> if none exists yet.</summary>
    Task<BusinessSettings?> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created business settings record.</summary>
    Task AddAsync(BusinessSettings settings, CancellationToken cancellationToken = default);
}
