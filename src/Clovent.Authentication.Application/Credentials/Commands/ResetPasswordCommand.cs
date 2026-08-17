using Clovent.Authentication.Credentials;
using Clovent.Authentication.Passwords;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Authentication.Application.Credentials.Commands;

/// <summary>
/// Admin-initiated password reset: sets a new password without requiring
/// the current one - authorization-gated in the caller (Desktop's
/// <c>users.resetpassword</c> feature check), not here. Distinct from
/// <see cref="ChangePasswordCommand"/> only in that it skips the
/// current-password verification step; both call the same
/// <see cref="UserCredentials.SetPassword"/>.
/// </summary>
public sealed record ResetPasswordCommand(Guid UserId, string NewPassword) : IRequest;

/// <summary>Handles <see cref="ResetPasswordCommand"/>.</summary>
public sealed class ResetPasswordCommandHandler(IUserCredentialsRepository credentialsRepository, IPasswordHasher passwordHasher)
    : IRequestHandler<ResetPasswordCommand>
{
    /// <inheritdoc/>
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var credentials = await credentialsRepository.GetByUserIdAsync(userId, cancellationToken);
        if (credentials is null)
        {
            // A user created via Clovent.Identity.Application's
            // CreateUserCommand has no UserCredentials row yet - that
            // bounded context has no dependency on Authentication and so
            // cannot create one itself (mirrors how
            // DevelopmentUserSeedStartupTask must create both separately).
            // Treating "no credentials yet" as "create them now" rather
            // than a NotFoundException means Reset Password also serves as
            // the first-password-set step for a brand-new user - confirmed
            // via manual verification this was otherwise an unhandled
            // exception on every newly created user.
            credentials = UserCredentials.Create(userId, DateTimeOffset.UtcNow);
            await credentialsRepository.AddAsync(credentials, cancellationToken);
        }

        var policyResult = PasswordPolicy.Default.Evaluate(request.NewPassword);
        if (!policyResult.IsSatisfied)
            throw AuthenticationDomainException.PasswordPolicyViolated(policyResult.Violations);

        var hash = PasswordHash.Create(passwordHasher.Hash(request.NewPassword));
        credentials.SetPassword(hash, DateTimeOffset.UtcNow);
    }
}
