using Clovent.Identity.Organizations;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.MasterData.Settings;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IBusinessSettingsRepository"/>.</summary>
public sealed class BusinessSettingsRepository(MasterDataDbContext dbContext) : IBusinessSettingsRepository
{
    /// <inheritdoc/>
    public async Task<BusinessSettings?> GetByIdAsync(BusinessSettingsId id, CancellationToken cancellationToken = default) =>
        await dbContext.BusinessSettings.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<BusinessSettings?> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.BusinessSettings.FirstOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(BusinessSettings settings, CancellationToken cancellationToken = default) =>
        await dbContext.BusinessSettings.AddAsync(settings, cancellationToken).ConfigureAwait(false);
}
