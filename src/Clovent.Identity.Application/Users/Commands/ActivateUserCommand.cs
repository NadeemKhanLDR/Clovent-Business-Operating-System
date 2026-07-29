using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Identity.Application.Users.Commands;

/// <summary>Activates a user.</summary>
public sealed record ActivateUserCommand(Guid UserId) : IRequest<UserDto>;

/// <summary>Handles <see cref="ActivateUserCommand"/>.</summary>
public sealed class ActivateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<ActivateUserCommand, UserDto>
{
    /// <inheritdoc/>
    public async Task<UserDto> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.Activate();

        return UserDto.FromDomain(user);
    }
}
