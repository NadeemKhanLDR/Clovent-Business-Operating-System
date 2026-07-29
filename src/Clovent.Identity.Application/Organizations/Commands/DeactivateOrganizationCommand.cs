using Clovent.Identity.Application.Organizations.Dtos;
using Clovent.Identity.Organizations;
using MediatR;

namespace Clovent.Identity.Application.Organizations.Commands;

/// <summary>Deactivates an organization.</summary>
public sealed record DeactivateOrganizationCommand(Guid OrganizationId) : IRequest<OrganizationDto>;

/// <summary>Handles <see cref="DeactivateOrganizationCommand"/>.</summary>
public sealed class DeactivateOrganizationCommandHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<DeactivateOrganizationCommand, OrganizationDto>
{
    /// <inheritdoc/>
    public async Task<OrganizationDto> Handle(DeactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var id = new OrganizationId(request.OrganizationId);
        var organization = await organizationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), request.OrganizationId);

        organization.Deactivate();

        return OrganizationDto.FromDomain(organization);
    }
}
