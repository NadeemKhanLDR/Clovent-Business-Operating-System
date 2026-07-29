using Clovent.Identity.Application.Organizations.Dtos;
using Clovent.Identity.Organizations;
using MediatR;

namespace Clovent.Identity.Application.Organizations.Commands;

/// <summary>Activates an organization.</summary>
public sealed record ActivateOrganizationCommand(Guid OrganizationId) : IRequest<OrganizationDto>;

/// <summary>Handles <see cref="ActivateOrganizationCommand"/>.</summary>
public sealed class ActivateOrganizationCommandHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<ActivateOrganizationCommand, OrganizationDto>
{
    /// <inheritdoc/>
    public async Task<OrganizationDto> Handle(ActivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var id = new OrganizationId(request.OrganizationId);
        var organization = await organizationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), request.OrganizationId);

        organization.Activate();

        return OrganizationDto.FromDomain(organization);
    }
}
