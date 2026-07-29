using Clovent.Authentication.RefreshSessions.Events;
using Clovent.Authentication.Sessions;
using Clovent.Domain;

namespace Clovent.Authentication.RefreshSessions;

/// <summary>
/// A renewable credential tied to a <see cref="Session"/>, modeled purely as
/// a lifecycle (issued/rotated/revoked/expired) with a fixed expiry window -
/// it holds no token secret. Generating and comparing the actual opaque
/// token value is a security/Infrastructure concern for a later milestone;
/// this aggregate only enforces when a refresh is allowed to succeed.
/// </summary>
public sealed class RefreshSession : AggregateRoot<RefreshSessionId>
{
    /// <summary>The session this refresh session renews.</summary>
    public SessionId SessionId { get; }

    /// <summary>UTC instant this refresh session was issued.</summary>
    public DateTimeOffset IssuedAtUtc { get; }

    /// <summary>UTC instant after which this refresh session can no longer be used.</summary>
    public DateTimeOffset ExpiresAtUtc { get; }

    /// <summary>The refresh session's current lifecycle state.</summary>
    public RefreshSessionStatus Status { get; private set; }

    /// <summary>
    /// Takes <paramref name="status"/> explicitly (rather than hardcoding
    /// <see cref="RefreshSessionStatus.Active"/>) so this is the single,
    /// unambiguous constructor an EF Core Infrastructure implementation can
    /// bind to when materializing an existing refresh session from storage.
    /// </summary>
    private RefreshSession(RefreshSessionId id, SessionId sessionId, DateTimeOffset issuedAtUtc, DateTimeOffset expiresAtUtc, RefreshSessionStatus status)
    {
        Id = id;
        SessionId = sessionId;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = status;
    }

    /// <summary>Issues a new active refresh session for a session.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lifetime"/> is not positive.</exception>
    public static RefreshSession Issue(SessionId sessionId, TimeSpan lifetime, DateTimeOffset nowUtc)
    {
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Lifetime must be positive.");

        var refreshSession = new RefreshSession(RefreshSessionId.New(), sessionId, nowUtc, nowUtc + lifetime, RefreshSessionStatus.Active);
        refreshSession.AddDomainEvent(new RefreshSessionIssued(refreshSession.Id, refreshSession.SessionId, nowUtc));
        return refreshSession;
    }

    /// <summary>
    /// Consumes this refresh session and issues its replacement for the same
    /// session (single-use rotation) - the standard mitigation for refresh
    /// token replay.
    /// </summary>
    /// <exception cref="AuthenticationDomainException">This refresh session is not active, or has already expired.</exception>
    public RefreshSession Rotate(TimeSpan newLifetime, DateTimeOffset nowUtc)
    {
        if (Status != RefreshSessionStatus.Active)
            throw AuthenticationDomainException.RefreshSessionNotActive(Id);
        if (nowUtc >= ExpiresAtUtc)
            throw AuthenticationDomainException.RefreshSessionExpired(Id);

        var replacement = Issue(SessionId, newLifetime, nowUtc);
        Status = RefreshSessionStatus.Rotated;
        AddDomainEvent(new RefreshSessionRotated(Id, replacement.Id, nowUtc));
        return replacement;
    }

    /// <summary>Administratively terminates the refresh session.</summary>
    /// <exception cref="AuthenticationDomainException">This refresh session is not active.</exception>
    public void Revoke(DateTimeOffset nowUtc)
    {
        if (Status != RefreshSessionStatus.Active)
            throw AuthenticationDomainException.RefreshSessionNotActive(Id);

        Status = RefreshSessionStatus.Revoked;
        AddDomainEvent(new RefreshSessionRevoked(Id, nowUtc));
    }

    /// <summary>Transitions the refresh session to expired because its expiry instant was reached.</summary>
    /// <exception cref="AuthenticationDomainException">This refresh session is not active, or its expiry instant has not yet been reached.</exception>
    public void Expire(DateTimeOffset nowUtc)
    {
        if (Status != RefreshSessionStatus.Active)
            throw AuthenticationDomainException.RefreshSessionNotActive(Id);
        if (nowUtc < ExpiresAtUtc)
            throw AuthenticationDomainException.RefreshSessionNotYetExpired(Id);

        Status = RefreshSessionStatus.Expired;
        AddDomainEvent(new RefreshSessionExpired(Id, nowUtc));
    }

    /// <summary>
    /// Invalidates this refresh session because the <see cref="Session"/> it
    /// belongs to ended (revoked, expired, or logged out). Unlike
    /// <see cref="Revoke"/>, this is idempotent - it silently does nothing if
    /// the refresh session is already inactive (already rotated, revoked, or
    /// expired). A cascade triggered by another aggregate's state change must
    /// never fail just because this one had already independently reached a
    /// terminal state (e.g. it was rotated moments earlier).
    /// </summary>
    public void Invalidate(DateTimeOffset nowUtc)
    {
        if (Status != RefreshSessionStatus.Active)
            return;

        Status = RefreshSessionStatus.Revoked;
        AddDomainEvent(new RefreshSessionRevoked(Id, nowUtc));
    }
}
