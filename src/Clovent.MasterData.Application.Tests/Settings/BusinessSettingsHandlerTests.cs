using Clovent.Identity.Organizations;
using Clovent.MasterData.Application.Settings.Commands;
using Clovent.MasterData.Application.Settings.Queries;
using Clovent.MasterData.Application.Tests.TestSupport;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Settings;
using Clovent.MasterData.TimeZones;
using Xunit;

namespace Clovent.MasterData.Application.Tests.Settings;

public class BusinessSettingsHandlerTests
{
    [Fact]
    public async Task CreateBusinessSettingsCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeBusinessSettingsRepository();
        var handler = new CreateBusinessSettingsCommandHandler(repository);
        var organizationId = OrganizationId.New();

        var dto = await handler.Handle(
            new CreateBusinessSettingsCommand(organizationId.Value, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "MM/dd/yyyy"),
            CancellationToken.None);

        Assert.Equal(organizationId.Value, dto.OrganizationId);
        Assert.Null(dto.DefaultFiscalYearId);
        Assert.NotNull(await repository.GetByIdAsync(new BusinessSettingsId(dto.BusinessSettingsId)));
    }

    [Fact]
    public async Task UpdateBusinessSettingsCommandHandler_SetsFiscalYearAndDateFormat()
    {
        var repository = new FakeBusinessSettingsRepository();
        var settings = BusinessSettings.Create(OrganizationId.New(), CurrencyId.New(), LanguageId.New(), TimeZoneEntryId.New(), "MM/dd/yyyy");
        repository.Add(settings);
        var handler = new UpdateBusinessSettingsCommandHandler(repository);
        var fiscalYearId = FiscalYearId.New();

        var dto = await handler.Handle(
            new UpdateBusinessSettingsCommand(settings.Id.Value, settings.DefaultCurrencyId.Value, settings.DefaultLanguageId.Value, settings.DefaultTimeZoneId.Value, fiscalYearId.Value, "dd/MM/yyyy"),
            CancellationToken.None);

        Assert.Equal(fiscalYearId.Value, dto.DefaultFiscalYearId);
        Assert.Equal("dd/MM/yyyy", dto.DateFormat);
    }

    [Fact]
    public async Task UpdateBusinessSettingsCommandHandler_UnknownSettings_Throws()
    {
        var handler = new UpdateBusinessSettingsCommandHandler(new FakeBusinessSettingsRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateBusinessSettingsCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "MM/dd/yyyy"), CancellationToken.None));
    }

    [Fact]
    public async Task GetBusinessSettingsByOrganizationQueryHandler_UnknownOrganization_Throws()
    {
        var handler = new GetBusinessSettingsByOrganizationQueryHandler(new FakeBusinessSettingsRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetBusinessSettingsByOrganizationQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task GetBusinessSettingsByOrganizationQueryHandler_ExistingSettings_ReturnsDto()
    {
        var repository = new FakeBusinessSettingsRepository();
        var organizationId = OrganizationId.New();
        var settings = BusinessSettings.Create(organizationId, CurrencyId.New(), LanguageId.New(), TimeZoneEntryId.New(), "MM/dd/yyyy");
        repository.Add(settings);
        var handler = new GetBusinessSettingsByOrganizationQueryHandler(repository);

        var dto = await handler.Handle(new GetBusinessSettingsByOrganizationQuery(organizationId.Value), CancellationToken.None);

        Assert.Equal(settings.Id.Value, dto.BusinessSettingsId);
    }
}
