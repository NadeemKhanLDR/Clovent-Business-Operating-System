using Clovent.Authentication.Credentials;
using Clovent.Authentication.Passwords;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Authentication.Application.Credentials.Commands;

/// <summary>
/// Self-service password change: requires the caller to prove they know the
/// current password before a new one is accepted. Distinct from
/// <see cref="ResetPasswordCommand"/> (the admin path, which skips this
/// check) - both ultimately call the same <see cref="UserCredentials.SetPassword"/>.
/// </summary>
public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;

/// <summary>Handles <see cref="ChangePasswordCommand"/>.</summary>
public sealed class ChangePasswordCommandHandler(IUserCredentialsRepository credentialsRepository, IPasswordHasher passwordHasher)
    : IRequestHandler<ChangePasswordCommand>
{
    /// <inheritdoc/>
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var credentials = await credentialsRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserCredentials), userId);

        if (credentials.PasswordHash is null || !passwordHasher.Verify(request.CurrentPassword, credentials.PasswordHash.Value))
            throw AuthenticationDomainException.CurrentPasswordIncorrect();

        var policyResult = PasswordPolicy.Default.Evaluate(request.NewPassword);
        if (!policyResult.IsSatisfied)
            throw AuthenticationDomainException.PasswordPolicyViolated(policyResult.Violations);

        var hash = PasswordHash.Create(passwordHasher.Hash(request.NewPassword));
        credentials.SetPassword(hash, DateTimeOffset.UtcNow);
    }
}
