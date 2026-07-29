using Clovent.Identity.Organizations;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.MasterData.Settings;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IBusinessSettingsRepository"/>.</summary>
public sealed class BusinessSettingsRepository(MasterDataDbContext dbContext) : IBusinessSettingsRepository
{
    /// <inheritdoc/>
    public Task<BusinessSettings?> GetByIdAsync(BusinessSettingsId id, CancellationToken cancellationToken = default) =>
        dbContext.BusinessSettings.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<BusinessSettings?> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default) =>
        dbContext.BusinessSettings.FirstOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(BusinessSettings settings, CancellationToken cancellationToken = default) =>
        await dbContext.BusinessSettings.AddAsync(settings, cancellationToken);
}
