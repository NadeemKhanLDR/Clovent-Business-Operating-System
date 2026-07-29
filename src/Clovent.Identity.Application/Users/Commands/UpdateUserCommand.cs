using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Users.Commands;

/// <summary>Changes the display name shown for a user - the only field <see cref="User"/> exposes a mutator for beyond lifecycle/assignment.</summary>
public sealed record UpdateUserCommand(Guid UserId, string DisplayName) : IRequest<UserDto>;

/// <summary>Handles <see cref="UpdateUserCommand"/>.</summary>
public sealed class UpdateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<UpdateUserCommand, UserDto>
{
    /// <inheritdoc/>
    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.ChangeDisplayName(DisplayName.Create(request.DisplayName));

        return UserDto.FromDomain(user);
    }
}
