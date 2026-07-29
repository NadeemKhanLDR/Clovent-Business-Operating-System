using Clovent.Authentication.Credentials;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Authentication.Application.Credentials.Commands;

/// <summary>
/// Unlocks a locked-out user: resets the Authentication-side failed-attempt
/// counter and unlocks the Identity-side <c>User</c> via
/// <see cref="IIdentityUserService"/> - the same cross-boundary seam
/// <c>LockUserAsync</c> already uses, now given its symmetric counterpart.
/// </summary>
public sealed record UnlockUserCommand(Guid UserId) : IRequest;

/// <summary>Handles <see cref="UnlockUserCommand"/>.</summary>
public sealed class UnlockUserCommandHandler(IUserCredentialsRepository credentialsRepository, IIdentityUserService identityUserService)
    : IRequestHandler<UnlockUserCommand>
{
    /// <inheritdoc/>
    public async Task Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var credentials = await credentialsRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserCredentials), userId);

        credentials.ResetFailedAttempts();
        await identityUserService.UnlockUserAsync(request.UserId, cancellationToken);
    }
}
