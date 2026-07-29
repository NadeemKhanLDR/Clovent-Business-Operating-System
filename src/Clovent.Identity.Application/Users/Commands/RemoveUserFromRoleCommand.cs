using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Identity.Application.Users.Commands;

/// <summary>Removes a role from a user.</summary>
public sealed record RemoveUserFromRoleCommand(Guid UserId, Guid RoleId) : IRequest<UserDto>;

/// <summary>Handles <see cref="RemoveUserFromRoleCommand"/>.</summary>
public sealed class RemoveUserFromRoleCommandHandler(IUserRepository userRepository)
    : IRequestHandler<RemoveUserFromRoleCommand, UserDto>
{
    /// <inheritdoc/>
    public async Task<UserDto> Handle(RemoveUserFromRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.RemoveRole(new RoleId(request.RoleId));

        return UserDto.FromDomain(user);
    }
}
