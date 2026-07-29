using Clovent.Identity.Application.Permissions.Dtos;
using Clovent.Identity.Permissions;
using MediatR;

namespace Clovent.Identity.Application.Permissions.Queries;

/// <summary>Retrieves every permission - reference/catalog data, used to populate a role's permission checklist.</summary>
public sealed record ListPermissionsQuery : IRequest<IReadOnlyCollection<PermissionDto>>;

/// <summary>Handles <see cref="ListPermissionsQuery"/>.</summary>
public sealed class ListPermissionsQueryHandler(IPermissionRepository permissionRepository)
    : IRequestHandler<ListPermissionsQuery, IReadOnlyCollection<PermissionDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<PermissionDto>> Handle(ListPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await permissionRepository.ListAllAsync(cancellationToken);
        return [.. permissions.Select(PermissionDto.FromDomain)];
    }
}
