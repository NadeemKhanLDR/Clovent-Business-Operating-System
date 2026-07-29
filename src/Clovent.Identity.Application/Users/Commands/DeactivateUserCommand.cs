using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Identity.Application.Users.Commands;

/// <summary>Deactivates a user.</summary>
public sealed record DeactivateUserCommand(Guid UserId) : IRequest<UserDto>;

/// <summary>Handles <see cref="DeactivateUserCommand"/>.</summary>
public sealed class DeactivateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<DeactivateUserCommand, UserDto>
{
    /// <inheritdoc/>
    public async Task<UserDto> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.Deactivate();

        return UserDto.FromDomain(user);
    }
}
