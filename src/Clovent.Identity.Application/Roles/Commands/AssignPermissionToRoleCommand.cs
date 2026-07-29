using Clovent.Identity.Application.Roles.Dtos;
using Clovent.Identity.Permissions;
using Clovent.Identity.Roles;
using MediatR;

namespace Clovent.Identity.Application.Roles.Commands;

/// <summary>Grants a permission to a role.</summary>
public sealed record AssignPermissionToRoleCommand(Guid RoleId, Guid PermissionId) : IRequest<RoleDto>;

/// <summary>Handles <see cref="AssignPermissionToRoleCommand"/>.</summary>
public sealed class AssignPermissionToRoleCommandHandler(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    : IRequestHandler<AssignPermissionToRoleCommand, RoleDto>
{
    /// <inheritdoc/>
    public async Task<RoleDto> Handle(AssignPermissionToRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(new RoleId(request.RoleId), cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        var permissionId = new PermissionId(request.PermissionId);
        _ = await permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Permission), request.PermissionId);

        role.AddPermission(permissionId);

        return RoleDto.FromDomain(role);
    }
}
