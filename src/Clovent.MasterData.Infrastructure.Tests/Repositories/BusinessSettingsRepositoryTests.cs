using Clovent.Identity.Organizations;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Infrastructure.Repositories;
using Clovent.MasterData.Infrastructure.Tests.TestSupport;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Settings;
using Clovent.MasterData.TimeZones;
using Xunit;

namespace Clovent.MasterData.Infrastructure.Tests.Repositories;

public class BusinessSettingsRepositoryTests : SqliteTestBase
{
    private static BusinessSettings CreateSettings(OrganizationId organizationId) =>
        BusinessSettings.Create(organizationId, CurrencyId.New(), LanguageId.New(), TimeZoneEntryId.New(), "MM/dd/yyyy");

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var organizationId = OrganizationId.New();
        var settings = CreateSettings(organizationId);

        await using (var writeContext = CreateContext())
        {
            var repository = new BusinessSettingsRepository(writeContext);
            await repository.AddAsync(settings);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new BusinessSettingsRepository(readContext).GetByIdAsync(settings.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(organizationId, reloaded!.OrganizationId);
        Assert.Equal(settings.DefaultCurrencyId, reloaded.DefaultCurrencyId);
        Assert.Equal(settings.DateFormat, reloaded.DateFormat);
        Assert.Null(reloaded.DefaultFiscalYearId);
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_FindsMatch()
    {
        var organizationId = OrganizationId.New();
        var settings = CreateSettings(organizationId);

        await using (var writeContext = CreateContext())
        {
            var repository = new BusinessSettingsRepository(writeContext);
            await repository.AddAsync(settings);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new BusinessSettingsRepository(readContext).GetByOrganizationIdAsync(organizationId);

        Assert.NotNull(found);
        Assert.Equal(settings.Id, found!.Id);
    }

    [Fact]
    public async Task UpdateDefaults_ThenReload_PersistsFiscalYearId()
    {
        var settings = CreateSettings(OrganizationId.New());
        var fiscalYearId = FiscalYears.FiscalYearId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new BusinessSettingsRepository(writeContext);
            await repository.AddAsync(settings);
            await writeContext.SaveChangesAsync();
        }

        await using (var updateContext = CreateContext())
        {
            var repository = new BusinessSettingsRepository(updateContext);
            var loaded = await repository.GetByIdAsync(settings.Id);
            loaded!.UpdateDefaults(loaded.DefaultCurrencyId, loaded.DefaultLanguageId, loaded.DefaultTimeZoneId, fiscalYearId, "dd/MM/yyyy");
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new BusinessSettingsRepository(readContext).GetByIdAsync(settings.Id);

        Assert.Equal(fiscalYearId, reloaded!.DefaultFiscalYearId);
        Assert.Equal("dd/MM/yyyy", reloaded.DateFormat);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new BusinessSettingsRepository(context).GetByIdAsync(BusinessSettingsId.New());

        Assert.Null(result);
    }
}
