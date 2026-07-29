using Clovent.Identity.Application.Organizations.Dtos;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Organizations.Commands;

/// <summary>Renames an existing organization.</summary>
public sealed record RenameOrganizationCommand(Guid OrganizationId, string Name) : IRequest<OrganizationDto>;

/// <summary>Handles <see cref="RenameOrganizationCommand"/>.</summary>
public sealed class RenameOrganizationCommandHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<RenameOrganizationCommand, OrganizationDto>
{
    /// <inheritdoc/>
    public async Task<OrganizationDto> Handle(RenameOrganizationCommand request, CancellationToken cancellationToken)
    {
        var id = new OrganizationId(request.OrganizationId);
        var organization = await organizationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), request.OrganizationId);

        organization.Rename(OrganizationName.Create(request.Name));

        return OrganizationDto.FromDomain(organization);
    }
}
