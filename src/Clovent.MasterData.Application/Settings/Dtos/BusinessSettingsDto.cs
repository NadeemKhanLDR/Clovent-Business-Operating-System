using Clovent.MasterData.Settings;

namespace Clovent.MasterData.Application.Settings.Dtos;

/// <summary>Read-model shape for a <see cref="BusinessSettings"/> record, safe to cross a process boundary.</summary>
public sealed record BusinessSettingsDto(
    Guid BusinessSettingsId,
    Guid OrganizationId,
    Guid DefaultCurrencyId,
    Guid DefaultLanguageId,
    Guid DefaultTimeZoneId,
    Guid? DefaultFiscalYearId,
    string DateFormat,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    /// <summary>Projects a domain <see cref="BusinessSettings"/> into its DTO.</summary>
    public static BusinessSettingsDto FromDomain(BusinessSettings settings) => new(
        settings.Id.Value,
        settings.OrganizationId.Value,
        settings.DefaultCurrencyId.Value,
        settings.DefaultLanguageId.Value,
        settings.DefaultTimeZoneId.Value,
        settings.DefaultFiscalYearId?.Value,
        settings.DateFormat,
        settings.CreatedAtUtc,
        settings.UpdatedAtUtc);
}
