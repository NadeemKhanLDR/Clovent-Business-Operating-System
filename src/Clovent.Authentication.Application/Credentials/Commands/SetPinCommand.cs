using Clovent.Authentication.Credentials;
using Clovent.Authentication.Pins;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Authentication.Application.Credentials.Commands;

/// <summary>
/// Admin-initiated PIN assignment: sets (or, when <c>Pin</c> is null/blank,
/// clears) a user's POS sign-in PIN - authorization-gated in the caller
/// (Desktop's <c>users.setpin</c> feature check), not here, exactly like
/// <see cref="ResetPasswordCommand"/>. A null/blank <c>Pin</c> clears the
/// credential entirely rather than storing an "empty PIN", so a user cannot
/// be left with a guessable zero-content PIN.
/// </summary>
/// <remarks>
/// PINs are unique across users: PIN-only sign-in (no username) resolves the
/// user FROM the PIN, so a duplicate would make authentication ambiguous.
/// Hashes are salted, so uniqueness cannot be a database index - it is
/// enforced here by verifying the candidate against every other user's
/// stored <see cref="UserCredentials.PinHash"/> before assigning it.
/// </remarks>
public sealed record SetPinCommand(Guid UserId, string? Pin) : IRequest;

/// <summary>Handles <see cref="SetPinCommand"/>.</summary>
public sealed class SetPinCommandHandler(IUserCredentialsRepository credentialsRepository, IPinHasher pinHasher)
    : IRequestHandler<SetPinCommand>
{
    /// <inheritdoc/>
    public async Task Handle(SetPinCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var credentials = await credentialsRepository.GetByUserIdAsync(userId, cancellationToken);
        if (credentials is null)
        {
            // A user created via Clovent.Identity.Application's
            // CreateUserCommand has no UserCredentials row yet - same
            // create-on-demand as ResetPasswordCommandHandler.
            credentials = UserCredentials.Create(userId, DateTimeOffset.UtcNow);
            await credentialsRepository.AddAsync(credentials, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(request.Pin))
        {
            credentials.ClearPin(DateTimeOffset.UtcNow);
            return;
        }

        var policyResult = PinPolicy.Default.Evaluate(request.Pin);
        if (!policyResult.IsSatisfied)
            throw AuthenticationDomainException.PinPolicyViolated(policyResult.Violations);

        // Uniqueness scan: salted hashes give no lookup key, so verify the
        // candidate against every other user's PIN hash and reject on a match.
        var all = await credentialsRepository.GetAllAsync(cancellationToken);
        foreach (var other in all)
        {
            if (other.UserId.Equals(userId) || other.PinHash is null)
                continue;

            if (pinHasher.Verify(request.Pin, other.PinHash.Value))
                throw AuthenticationDomainException.PinAlreadyInUse();
        }

        var hash = PinHash.Create(pinHasher.Hash(request.Pin));
        credentials.SetPin(hash, DateTimeOffset.UtcNow);
    }
}
