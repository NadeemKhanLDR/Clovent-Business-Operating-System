using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Identity.Application.Users.Commands;

/// <summary>Assigns a role to a user.</summary>
public sealed record AssignUserToRoleCommand(Guid UserId, Guid RoleId) : IRequest<UserDto>;

/// <summary>Handles <see cref="AssignUserToRoleCommand"/>.</summary>
public sealed class AssignUserToRoleCommandHandler(IUserRepository userRepository, IRoleRepository roleRepository)
    : IRequestHandler<AssignUserToRoleCommand, UserDto>
{
    /// <inheritdoc/>
    public async Task<UserDto> Handle(AssignUserToRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var roleId = new RoleId(request.RoleId);
        _ = await roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        user.AssignRole(roleId);

        return UserDto.FromDomain(user);
    }
}
