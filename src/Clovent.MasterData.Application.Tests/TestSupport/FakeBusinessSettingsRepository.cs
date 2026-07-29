using Clovent.Identity.Organizations;
using Clovent.MasterData.Settings;

namespace Clovent.MasterData.Application.Tests.TestSupport;

internal sealed class FakeBusinessSettingsRepository : IBusinessSettingsRepository
{
    private readonly Dictionary<BusinessSettingsId, BusinessSettings> _settings = [];

    public void Add(BusinessSettings settings) => _settings[settings.Id] = settings;

    public Task<BusinessSettings?> GetByIdAsync(BusinessSettingsId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_settings.GetValueOrDefault(id));

    public Task<BusinessSettings?> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_settings.Values.FirstOrDefault(s => s.OrganizationId == organizationId));

    public Task AddAsync(BusinessSettings settings, CancellationToken cancellationToken = default)
    {
        _settings[settings.Id] = settings;
        return Task.CompletedTask;
    }
}
