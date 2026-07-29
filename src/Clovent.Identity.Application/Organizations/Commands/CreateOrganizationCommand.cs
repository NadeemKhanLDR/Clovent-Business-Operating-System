using Clovent.Identity.Application.Organizations.Dtos;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.ValueObjects;
using Clovent.Identity.Shared.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Organizations.Commands;

/// <summary>Creates a new organization.</summary>
public sealed record CreateOrganizationCommand(string Name, string? TaxId = null) : IRequest<OrganizationDto>;

/// <summary>Handles <see cref="CreateOrganizationCommand"/>.</summary>
public sealed class CreateOrganizationCommandHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<CreateOrganizationCommand, OrganizationDto>
{
    /// <inheritdoc/>
    public async Task<OrganizationDto> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var taxId = request.TaxId is null ? null : TaxId.Create(request.TaxId);
        var organization = Organization.Create(OrganizationName.Create(request.Name), taxId);

        await organizationRepository.AddAsync(organization, cancellationToken);

        return OrganizationDto.FromDomain(organization);
    }
}
