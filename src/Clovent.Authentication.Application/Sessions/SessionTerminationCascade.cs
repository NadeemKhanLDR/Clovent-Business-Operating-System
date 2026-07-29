using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;

namespace Clovent.Authentication.Application.Sessions;

/// <summary>
/// Enforces the cross-aggregate rule "when a Session ends, its active
/// RefreshSession must become invalid too." <see cref="Session"/> and
/// <see cref="RefreshSession"/> are independent aggregate roots, each with
/// its own repository - neither can reach into the other directly, so the
/// rule is enforced here, at the Application layer, by whichever command
/// handler just ended the session. Shared by every session-termination
/// handler (<c>RevokeSessionCommand</c>, <c>ExpireSessionCommand</c>,
/// <c>LogOutSessionCommand</c>) so the rule is applied identically no
/// matter how the session ended.
/// </summary>
public sealed class SessionTerminationCascade(IRefreshSessionRepository refreshSessionRepository)
{
    /// <summary>Invalidates the active refresh session for <paramref name="sessionId"/>, if any.</summary>
    public async Task ApplyAsync(SessionId sessionId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var refreshSession = await refreshSessionRepository.GetActiveBySessionIdAsync(sessionId, cancellationToken);
        refreshSession?.Invalidate(nowUtc);
    }
}
