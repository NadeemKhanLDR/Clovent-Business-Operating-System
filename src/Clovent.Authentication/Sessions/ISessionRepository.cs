using Clovent.Identity.Users;

namespace Clovent.Authentication.Sessions;

/// <summary>
/// Persistence contract for <see cref="Session"/> aggregates. No
/// implementation exists yet - this is the seam a future
/// Infrastructure/Persistence milestone implements against.
/// </summary>
public interface ISessionRepository
{
    /// <summary>Retrieves a session by identity, or <see langword="null"/> if none exists.</summary>
    Task<Session?> GetByIdAsync(SessionId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every currently-active session for a user.</summary>
    Task<IReadOnlyCollection<Session>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-started session.</summary>
    Task AddAsync(Session session, CancellationToken cancellationToken = default);
}
