using Clovent.Identity.Application.Organizations.Dtos;
using Clovent.Identity.Organizations;
using MediatR;

namespace Clovent.Identity.Application.Organizations.Queries;

/// <summary>Retrieves every organization.</summary>
public sealed record ListOrganizationsQuery : IRequest<IReadOnlyCollection<OrganizationDto>>;

/// <summary>Handles <see cref="ListOrganizationsQuery"/>.</summary>
public sealed class ListOrganizationsQueryHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<ListOrganizationsQuery, IReadOnlyCollection<OrganizationDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<OrganizationDto>> Handle(ListOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var organizations = await organizationRepository.GetAllAsync(cancellationToken);
        return [.. organizations.Select(OrganizationDto.FromDomain)];
    }
}
