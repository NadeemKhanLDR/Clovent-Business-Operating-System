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
        var credentials = await credentialsRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserCredentials), userId);

        var policyResult = PasswordPolicy.Default.Evaluate(request.NewPassword);
        if (!policyResult.IsSatisfied)
            throw AuthenticationDomainException.PasswordPolicyViolated(policyResult.Violations);

        var hash = PasswordHash.Create(passwordHasher.Hash(request.NewPassword));
        credentials.SetPassword(hash, DateTimeOffset.UtcNow);
    }
}
