using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Users.Commands;

/// <summary>Creates a new user, in <see cref="UserStatus.PendingActivation"/> with no roles/company/branch assigned.</summary>
public sealed record CreateUserCommand(string Email, string UserName, string DisplayName) : IRequest<UserDto>;

/// <summary>Handles <see cref="CreateUserCommand"/>.</summary>
public sealed class CreateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<CreateUserCommand, UserDto>
{
    /// <inheritdoc/>
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var userName = UserName.Create(request.UserName);
        var displayName = DisplayName.Create(request.DisplayName);

        if (await userRepository.GetByEmailAsync(email, cancellationToken) is not null)
            throw IdentityDomainException.EmailAlreadyInUse(email.Value);

        if (await userRepository.GetByUserNameAsync(userName, cancellationToken) is not null)
            throw IdentityDomainException.UserNameAlreadyInUse(userName.Value);

        var user = User.Create(email, userName, displayName);
        await userRepository.AddAsync(user, cancellationToken);

        return UserDto.FromDomain(user);
    }
}
