using Clovent.Identity.Users;

namespace Clovent.Authentication.LoginAttempts;

/// <summary>
/// Persistence contract for <see cref="LoginAttempt"/> aggregates. No
/// implementation exists yet - this is the seam a future
/// Infrastructure/Persistence milestone implements against.
/// </summary>
public interface ILoginAttemptRepository
{
    /// <summary>Retrieves a login attempt by identity, or <see langword="null"/> if none exists.</summary>
    Task<LoginAttempt?> GetByIdAsync(LoginAttemptId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves attempts recorded against the given identifier since <paramref name="sinceUtc"/>,
    /// most recent first - the input a lockout policy evaluates.
    /// </summary>
    Task<IReadOnlyCollection<LoginAttempt>> GetRecentByIdentifierAsync(
        string attemptedIdentifier,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves attempts recorded against a known user since <paramref name="sinceUtc"/>, most recent first.</summary>
    Task<IReadOnlyCollection<LoginAttempt>> GetRecentByUserIdAsync(
        UserId userId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-recorded login attempt.</summary>
    Task AddAsync(LoginAttempt attempt, CancellationToken cancellationToken = default);
}
