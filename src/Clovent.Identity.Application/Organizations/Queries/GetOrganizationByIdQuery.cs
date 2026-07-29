using Clovent.Identity.Application.Organizations.Dtos;
using Clovent.Identity.Organizations;
using MediatR;

namespace Clovent.Identity.Application.Organizations.Queries;

/// <summary>Retrieves a single organization by identity.</summary>
public sealed record GetOrganizationByIdQuery(Guid OrganizationId) : IRequest<OrganizationDto>;

/// <summary>Handles <see cref="GetOrganizationByIdQuery"/>.</summary>
public sealed class GetOrganizationByIdQueryHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<GetOrganizationByIdQuery, OrganizationDto>
{
    /// <inheritdoc/>
    public async Task<OrganizationDto> Handle(GetOrganizationByIdQuery request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(new OrganizationId(request.OrganizationId), cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), request.OrganizationId);

        return OrganizationDto.FromDomain(organization);
    }
}
