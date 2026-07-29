using Clovent.MasterData.Application.Settings.Dtos;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Settings;
using Clovent.MasterData.TimeZones;
using MediatR;

namespace Clovent.MasterData.Application.Settings.Commands;

/// <summary>Updates an organization's business settings defaults.</summary>
public sealed record UpdateBusinessSettingsCommand(
    Guid BusinessSettingsId,
    Guid DefaultCurrencyId,
    Guid DefaultLanguageId,
    Guid DefaultTimeZoneId,
    Guid? DefaultFiscalYearId,
    string DateFormat) : IRequest<BusinessSettingsDto>;

/// <summary>Handles <see cref="UpdateBusinessSettingsCommand"/>.</summary>
public sealed class UpdateBusinessSettingsCommandHandler(IBusinessSettingsRepository businessSettingsRepository)
    : IRequestHandler<UpdateBusinessSettingsCommand, BusinessSettingsDto>
{
    /// <inheritdoc/>
    public async Task<BusinessSettingsDto> Handle(UpdateBusinessSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await businessSettingsRepository.GetByIdAsync(new BusinessSettingsId(request.BusinessSettingsId), cancellationToken)
            ?? throw new NotFoundException(nameof(BusinessSettings), request.BusinessSettingsId);

        settings.UpdateDefaults(
            new CurrencyId(request.DefaultCurrencyId),
            new LanguageId(request.DefaultLanguageId),
            new TimeZoneEntryId(request.DefaultTimeZoneId),
            request.DefaultFiscalYearId is null ? null : new FiscalYearId(request.DefaultFiscalYearId.Value),
            request.DateFormat);

        return BusinessSettingsDto.FromDomain(settings);
    }
}
