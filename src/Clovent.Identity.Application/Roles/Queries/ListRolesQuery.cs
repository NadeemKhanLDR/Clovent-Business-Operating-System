using Clovent.Identity.Application.Roles.Dtos;
using Clovent.Identity.Roles;
using MediatR;

namespace Clovent.Identity.Application.Roles.Queries;

/// <summary>Retrieves every role.</summary>
public sealed record ListRolesQuery : IRequest<IReadOnlyCollection<RoleDto>>;

/// <summary>Handles <see cref="ListRolesQuery"/>.</summary>
public sealed class ListRolesQueryHandler(IRoleRepository roleRepository)
    : IRequestHandler<ListRolesQuery, IReadOnlyCollection<RoleDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<RoleDto>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.ListAllAsync(cancellationToken);
        return [.. roles.Select(RoleDto.FromDomain)];
    }
}
