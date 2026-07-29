using Clovent.Identity.Organizations;
using Clovent.MasterData.Application.Settings.Dtos;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Settings;
using Clovent.MasterData.TimeZones;
using MediatR;

namespace Clovent.MasterData.Application.Settings.Commands;

/// <summary>Creates the (one and only) business settings record for an organization.</summary>
public sealed record CreateBusinessSettingsCommand(
    Guid OrganizationId,
    Guid DefaultCurrencyId,
    Guid DefaultLanguageId,
    Guid DefaultTimeZoneId,
    string DateFormat) : IRequest<BusinessSettingsDto>;

/// <summary>Handles <see cref="CreateBusinessSettingsCommand"/>.</summary>
public sealed class CreateBusinessSettingsCommandHandler(IBusinessSettingsRepository businessSettingsRepository)
    : IRequestHandler<CreateBusinessSettingsCommand, BusinessSettingsDto>
{
    /// <inheritdoc/>
    public async Task<BusinessSettingsDto> Handle(CreateBusinessSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = BusinessSettings.Create(
            new OrganizationId(request.OrganizationId),
            new CurrencyId(request.DefaultCurrencyId),
            new LanguageId(request.DefaultLanguageId),
            new TimeZoneEntryId(request.DefaultTimeZoneId),
            request.DateFormat);

        await businessSettingsRepository.AddAsync(settings, cancellationToken);

        return BusinessSettingsDto.FromDomain(settings);
    }
}
