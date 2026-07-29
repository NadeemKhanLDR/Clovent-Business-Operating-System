using Clovent.Authentication.Application.LoginAttempts.Dtos;
using Clovent.Authentication.Lockouts;
using Clovent.Authentication.LoginAttempts;
using Clovent.Authentication.Shared.ValueObjects;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Authentication.Application.LoginAttempts.Commands;

/// <summary>
/// Records the outcome of a login attempt. When the outcome is a failure
/// against a known user, evaluates <see cref="LockoutPolicy"/> against that
/// user's recent failures and, when the threshold is met, locks the account
/// via <see cref="IIdentityUserService"/> - never by loading Identity's
/// <c>User</c> aggregate or calling <c>User.Lock()</c> directly.
/// </summary>
public sealed record RecordLoginAttemptCommand(
    string AttemptedIdentifier,
    Guid? UserId,
    LoginOutcome Outcome,
    string? IpAddress = null) : IRequest<LoginAttemptDto>;

/// <summary>Handles <see cref="RecordLoginAttemptCommand"/>.</summary>
public sealed class RecordLoginAttemptCommandHandler(
    ILoginAttemptRepository loginAttemptRepository,
    IIdentityUserService identityUserService,
    TimeProvider timeProvider,
    LockoutPolicy? lockoutPolicy = null) : IRequestHandler<RecordLoginAttemptCommand, LoginAttemptDto>
{
    private readonly LockoutPolicy _lockoutPolicy = lockoutPolicy ?? LockoutPolicy.Default;

    /// <inheritdoc/>
    public async Task<LoginAttemptDto> Handle(RecordLoginAttemptCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var userId = request.UserId is { } id ? new UserId(id) : (UserId?)null;
        var ipAddress = request.IpAddress is null ? null : IpAddress.Create(request.IpAddress);

        var attempt = LoginAttempt.Record(request.AttemptedIdentifier, userId, request.Outcome, now, ipAddress);
        await loginAttemptRepository.AddAsync(attempt, cancellationToken);

        if (attempt.IsFailure && userId is { } lockedUserId)
        {
            await ApplyLockoutPolicyAsync(lockedUserId, now, cancellationToken);
        }

        return LoginAttemptDto.FromDomain(attempt);
    }

    private async Task ApplyLockoutPolicyAsync(UserId userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var recentFailures = await loginAttemptRepository.GetRecentByUserIdAsync(
            userId, now - _lockoutPolicy.EvaluationWindow, cancellationToken);
        var recentFailureCount = recentFailures.Count(a => a.IsFailure);

        if (!_lockoutPolicy.ShouldLock(recentFailureCount))
            return;

        if (!await identityUserService.IsUserActiveAsync(userId.Value, cancellationToken))
            return;

        await identityUserService.LockUserAsync(userId.Value, cancellationToken);
    }
}
