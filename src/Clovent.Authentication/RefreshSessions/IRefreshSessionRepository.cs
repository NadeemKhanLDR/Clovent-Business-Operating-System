using Clovent.Authentication.Sessions;

namespace Clovent.Authentication.RefreshSessions;

/// <summary>
/// Persistence contract for <see cref="RefreshSession"/> aggregates. No
/// implementation exists yet - this is the seam a future
/// Infrastructure/Persistence milestone implements against.
/// </summary>
public interface IRefreshSessionRepository
{
    /// <summary>Retrieves a refresh session by identity, or <see langword="null"/> if none exists.</summary>
    Task<RefreshSession?> GetByIdAsync(RefreshSessionId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the currently-active refresh session for a session, if any.</summary>
    Task<RefreshSession?> GetActiveBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-issued refresh session.</summary>
    Task AddAsync(RefreshSession refreshSession, CancellationToken cancellationToken = default);
}
