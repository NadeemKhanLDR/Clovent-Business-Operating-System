using Clovent.Identity.Organizations;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Settings;
using Clovent.MasterData.Settings.Events;
using Clovent.MasterData.TimeZones;
using Xunit;

namespace Clovent.MasterData.Tests.Settings;

public class BusinessSettingsTests
{
    [Fact]
    public void Create_SetsFields_RaisesBusinessSettingsCreated()
    {
        var organizationId = OrganizationId.New();
        var currencyId = CurrencyId.New();
        var languageId = LanguageId.New();
        var timeZoneId = TimeZoneEntryId.New();

        var settings = BusinessSettings.Create(organizationId, currencyId, languageId, timeZoneId, "MM/dd/yyyy");

        Assert.Equal(organizationId, settings.OrganizationId);
        Assert.Equal(currencyId, settings.DefaultCurrencyId);
        Assert.Equal(languageId, settings.DefaultLanguageId);
        Assert.Equal(timeZoneId, settings.DefaultTimeZoneId);
        Assert.Null(settings.DefaultFiscalYearId);
        Assert.Equal("MM/dd/yyyy", settings.DateFormat);
        Assert.IsType<BusinessSettingsCreated>(Assert.Single(settings.DomainEvents));
    }

    [Fact]
    public void UpdateDefaults_ChangesFieldsAndTimestamp_RaisesBusinessSettingsUpdated()
    {
        var settings = BusinessSettings.Create(OrganizationId.New(), CurrencyId.New(), LanguageId.New(), TimeZoneEntryId.New(), "MM/dd/yyyy");
        settings.ClearDomainEvents();
        var newCurrencyId = CurrencyId.New();
        var fiscalYearId = FiscalYearId.New();

        settings.UpdateDefaults(newCurrencyId, settings.DefaultLanguageId, settings.DefaultTimeZoneId, fiscalYearId, "dd/MM/yyyy");

        Assert.Equal(newCurrencyId, settings.DefaultCurrencyId);
        Assert.Equal(fiscalYearId, settings.DefaultFiscalYearId);
        Assert.Equal("dd/MM/yyyy", settings.DateFormat);
        Assert.IsType<BusinessSettingsUpdated>(Assert.Single(settings.DomainEvents));
    }

    [Fact]
    public void Create_EmptyDateFormat_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BusinessSettings.Create(OrganizationId.New(), CurrencyId.New(), LanguageId.New(), TimeZoneEntryId.New(), ""));
    }
}
