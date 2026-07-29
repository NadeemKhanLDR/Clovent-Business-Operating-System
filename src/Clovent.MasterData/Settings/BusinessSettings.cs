using Clovent.Domain;
using Clovent.Identity.Organizations;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Settings.Events;
using Clovent.MasterData.TimeZones;

namespace Clovent.MasterData.Settings;

/// <summary>
/// The default currency/language/time zone/fiscal year for an
/// <see cref="Organization"/> - exactly one per organization (enforced by a
/// unique index at the Infrastructure layer, the same pattern already used
/// for <c>Clovent.Authentication.Credentials.UserCredentials</c>, one per
/// user). This is also where "current fiscal year" is actually decided
/// (<see cref="DefaultFiscalYearId"/>) - see <see cref="FiscalYears.FiscalYear"/>'s
/// doc comment for why no <c>IsCurrent</c> flag lives on <c>FiscalYear</c> itself.
/// </summary>
public sealed class BusinessSettings : AggregateRoot<BusinessSettingsId>
{
    private const int MaxDateFormatLength = 20;

    /// <summary>The organization these settings belong to, fixed at creation.</summary>
    public OrganizationId OrganizationId { get; }

    /// <summary>The organization's default currency.</summary>
    public CurrencyId DefaultCurrencyId { get; private set; }

    /// <summary>The organization's default language.</summary>
    public LanguageId DefaultLanguageId { get; private set; }

    /// <summary>The organization's default time zone.</summary>
    public TimeZoneEntryId DefaultTimeZoneId { get; private set; }

    /// <summary>The organization's current fiscal year, if one has been designated.</summary>
    public FiscalYearId? DefaultFiscalYearId { get; private set; }

    /// <summary>The organization's preferred date display format (e.g. "MM/dd/yyyy").</summary>
    public string DateFormat { get; private set; }

    /// <summary>UTC instant these settings were created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant these settings were last updated.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private BusinessSettings(
        BusinessSettingsId id,
        OrganizationId organizationId,
        CurrencyId defaultCurrencyId,
        LanguageId defaultLanguageId,
        TimeZoneEntryId defaultTimeZoneId,
        FiscalYearId? defaultFiscalYearId,
        string dateFormat,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        DefaultCurrencyId = defaultCurrencyId;
        DefaultLanguageId = defaultLanguageId;
        DefaultTimeZoneId = defaultTimeZoneId;
        DefaultFiscalYearId = defaultFiscalYearId;
        DateFormat = dateFormat;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Creates the (one and only) business settings record for an organization.</summary>
    /// <exception cref="ArgumentException"><paramref name="dateFormat"/> is empty or too long.</exception>
    public static BusinessSettings Create(
        OrganizationId organizationId,
        CurrencyId defaultCurrencyId,
        LanguageId defaultLanguageId,
        TimeZoneEntryId defaultTimeZoneId,
        string dateFormat)
    {
        dateFormat = RequireDateFormat(dateFormat);

        var now = DateTimeOffset.UtcNow;
        var settings = new BusinessSettings(
            BusinessSettingsId.New(), organizationId, defaultCurrencyId, defaultLanguageId, defaultTimeZoneId, null, dateFormat, now, now);
        settings.AddDomainEvent(new BusinessSettingsCreated(settings.Id, settings.OrganizationId, now));
        return settings;
    }

    /// <summary>Updates the organization's defaults. All fields are set together, as they typically are from one settings form.</summary>
    /// <exception cref="ArgumentException"><paramref name="dateFormat"/> is empty or too long.</exception>
    public void UpdateDefaults(
        CurrencyId defaultCurrencyId,
        LanguageId defaultLanguageId,
        TimeZoneEntryId defaultTimeZoneId,
        FiscalYearId? defaultFiscalYearId,
        string dateFormat)
    {
        dateFormat = RequireDateFormat(dateFormat);

        DefaultCurrencyId = defaultCurrencyId;
        DefaultLanguageId = defaultLanguageId;
        DefaultTimeZoneId = defaultTimeZoneId;
        DefaultFiscalYearId = defaultFiscalYearId;
        DateFormat = dateFormat;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        AddDomainEvent(new BusinessSettingsUpdated(Id, UpdatedAtUtc));
    }

    private static string RequireDateFormat(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Date format is required.", nameof(value));

        value = value.Trim();

        if (value.Length > MaxDateFormatLength)
            throw new ArgumentException($"Date format cannot exceed {MaxDateFormatLength} characters.", nameof(value));

        return value;
    }
}
