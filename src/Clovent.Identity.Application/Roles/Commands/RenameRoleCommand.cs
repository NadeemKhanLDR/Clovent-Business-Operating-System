using Clovent.Identity.Application.Roles.Dtos;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Roles.Commands;

/// <summary>Renames an existing role.</summary>
public sealed record RenameRoleCommand(Guid RoleId, string Name) : IRequest<RoleDto>;

/// <summary>Handles <see cref="RenameRoleCommand"/>.</summary>
public sealed class RenameRoleCommandHandler(IRoleRepository roleRepository)
    : IRequestHandler<RenameRoleCommand, RoleDto>
{
    /// <inheritdoc/>
    public async Task<RoleDto> Handle(RenameRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(new RoleId(request.RoleId), cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        role.Rename(RoleName.Create(request.Name));

        return RoleDto.FromDomain(role);
    }
}
