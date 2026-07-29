using Clovent.Identity.Application.Roles.Dtos;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Roles.Commands;

/// <summary>Creates a new role with no permissions granted.</summary>
public sealed record CreateRoleCommand(string Name) : IRequest<RoleDto>;

/// <summary>Handles <see cref="CreateRoleCommand"/>.</summary>
public sealed class CreateRoleCommandHandler(IRoleRepository roleRepository)
    : IRequestHandler<CreateRoleCommand, RoleDto>
{
    /// <inheritdoc/>
    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var name = RoleName.Create(request.Name);

        if (await roleRepository.GetByNameAsync(name, cancellationToken) is not null)
            throw Clovent.Identity.IdentityDomainException.RoleNameAlreadyInUse(name.Value);

        var role = Role.Create(name);
        await roleRepository.AddAsync(role, cancellationToken);

        return RoleDto.FromDomain(role);
    }
}
