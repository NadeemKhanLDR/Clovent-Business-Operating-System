using Clovent.Identity.Organizations;
using Clovent.MasterData.Application.Settings.Dtos;
using Clovent.MasterData.Settings;
using MediatR;

namespace Clovent.MasterData.Application.Settings.Queries;

/// <summary>Retrieves the business settings record for an organization.</summary>
public sealed record GetBusinessSettingsByOrganizationQuery(Guid OrganizationId) : IRequest<BusinessSettingsDto>;

/// <summary>Handles <see cref="GetBusinessSettingsByOrganizationQuery"/>.</summary>
public sealed class GetBusinessSettingsByOrganizationQueryHandler(IBusinessSettingsRepository businessSettingsRepository)
    : IRequestHandler<GetBusinessSettingsByOrganizationQuery, BusinessSettingsDto>
{
    /// <inheritdoc/>
    public async Task<BusinessSettingsDto> Handle(GetBusinessSettingsByOrganizationQuery request, CancellationToken cancellationToken)
    {
        var settings = await businessSettingsRepository.GetByOrganizationIdAsync(new OrganizationId(request.OrganizationId), cancellationToken)
            ?? throw new NotFoundException(nameof(BusinessSettings), request.OrganizationId);

        return BusinessSettingsDto.FromDomain(settings);
    }
}
