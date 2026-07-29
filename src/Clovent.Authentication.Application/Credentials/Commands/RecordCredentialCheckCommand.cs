using Clovent.Authentication.Credentials;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Authentication.Application.Credentials.Commands;

/// <summary>
/// Records the outcome of checking a submitted password/PIN against a
/// user's stored <see cref="UserCredentials"/> - increments or resets
/// <see cref="UserCredentials.FailedAttempts"/> accordingly. Separate from
/// <c>RecordLoginAttemptCommand</c> (which records the login *attempt* as an
/// audit fact): this command mutates the credential record itself, the
/// other appends an immutable <c>LoginAttempt</c>. A login flow issues both.
/// </summary>
public sealed record RecordCredentialCheckCommand(Guid UserId, bool Succeeded) : IRequest;

/// <summary>Handles <see cref="RecordCredentialCheckCommand"/>.</summary>
public sealed class RecordCredentialCheckCommandHandler(IUserCredentialsRepository userCredentialsRepository)
    : IRequestHandler<RecordCredentialCheckCommand>
{
    /// <inheritdoc/>
    public async Task Handle(RecordCredentialCheckCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var credentials = await userCredentialsRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserCredentials), userId);

        if (request.Succeeded)
        {
            credentials.ResetFailedAttempts();
        }
        else
        {
            credentials.RecordFailedAttempt();
        }
    }
}
