using Clovent.Identity.Application.Roles.Dtos;
using Clovent.Identity.Permissions;
using Clovent.Identity.Roles;
using MediatR;

namespace Clovent.Identity.Application.Roles.Commands;

/// <summary>Revokes a permission from a role.</summary>
public sealed record RemovePermissionFromRoleCommand(Guid RoleId, Guid PermissionId) : IRequest<RoleDto>;

/// <summary>Handles <see cref="RemovePermissionFromRoleCommand"/>.</summary>
public sealed class RemovePermissionFromRoleCommandHandler(IRoleRepository roleRepository)
    : IRequestHandler<RemovePermissionFromRoleCommand, RoleDto>
{
    /// <inheritdoc/>
    public async Task<RoleDto> Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(new RoleId(request.RoleId), cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        role.RemovePermission(new PermissionId(request.PermissionId));

        return RoleDto.FromDomain(role);
    }
}
