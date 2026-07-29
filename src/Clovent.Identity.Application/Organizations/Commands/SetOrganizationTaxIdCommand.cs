using Clovent.Identity.Application.Organizations.Dtos;
using Clovent.Identity.Organizations;
using Clovent.Identity.Shared.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Organizations.Commands;

/// <summary>Sets or clears an organization's tax id.</summary>
public sealed record SetOrganizationTaxIdCommand(Guid OrganizationId, string? TaxId) : IRequest<OrganizationDto>;

/// <summary>Handles <see cref="SetOrganizationTaxIdCommand"/>.</summary>
public sealed class SetOrganizationTaxIdCommandHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<SetOrganizationTaxIdCommand, OrganizationDto>
{
    /// <inheritdoc/>
    public async Task<OrganizationDto> Handle(SetOrganizationTaxIdCommand request, CancellationToken cancellationToken)
    {
        var id = new OrganizationId(request.OrganizationId);
        var organization = await organizationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), request.OrganizationId);

        organization.SetTaxId(request.TaxId is null ? null : TaxId.Create(request.TaxId));

        return OrganizationDto.FromDomain(organization);
    }
}
