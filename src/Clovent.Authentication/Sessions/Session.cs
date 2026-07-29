using Clovent.Authentication.Sessions.Events;
using Clovent.Authentication.Shared.ValueObjects;
using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Authentication.Sessions;

/// <summary>
/// A single authenticated session for a <see cref="UserId"/>. Uses a sliding
/// idle timeout: every <see cref="Touch"/> pushes <see cref="ExpiresAtUtc"/>
/// forward by the session's fixed <see cref="IdleTimeout"/>, so a session
/// only expires after a period of no activity, not at a fixed instant.
/// </summary>
public sealed class Session : AggregateRoot<SessionId>
{
    /// <summary>The user this session authenticates.</summary>
    public UserId UserId { get; }

    /// <summary>The IP address the session was started from, if known.</summary>
    public IpAddress? IpAddress { get; }

    /// <summary>How long the session may sit idle before it is eligible to expire.</summary>
    public TimeSpan IdleTimeout { get; }

    /// <summary>UTC instant the session was started.</summary>
    public DateTimeOffset StartedAtUtc { get; }

    /// <summary>UTC instant of the most recent activity on this session.</summary>
    public DateTimeOffset LastActivityAtUtc { get; private set; }

    /// <summary>UTC instant this session becomes eligible to expire if untouched.</summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>The session's current lifecycle state.</summary>
    public SessionStatus Status { get; private set; }

    /// <summary>
    /// Takes every persisted field explicitly (rather than deriving
    /// <see cref="LastActivityAtUtc"/>/<see cref="ExpiresAtUtc"/>/<see cref="Status"/>
    /// from just the start-up parameters) so this is the single, unambiguous
    /// constructor an EF Core Infrastructure implementation can bind to when
    /// materializing an existing session from storage - a constructor that
    /// only accepted "start" parameters would silently reset those three
    /// fields to their brand-new-session values on every load.
    /// </summary>
    private Session(
        SessionId id,
        UserId userId,
        IpAddress? ipAddress,
        TimeSpan idleTimeout,
        DateTimeOffset startedAtUtc,
        DateTimeOffset lastActivityAtUtc,
        DateTimeOffset expiresAtUtc,
        SessionStatus status)
    {
        Id = id;
        UserId = userId;
        IpAddress = ipAddress;
        IdleTimeout = idleTimeout;
        StartedAtUtc = startedAtUtc;
        LastActivityAtUtc = lastActivityAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = status;
    }

    /// <summary>Starts a new active session for a user.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="idleTimeout"/> is not positive.</exception>
    public static Session Start(UserId userId, TimeSpan idleTimeout, DateTimeOffset nowUtc, IpAddress? ipAddress = null)
    {
        if (idleTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(idleTimeout), idleTimeout, "Idle timeout must be positive.");

        var session = new Session(SessionId.New(), userId, ipAddress, idleTimeout, nowUtc, nowUtc, nowUtc + idleTimeout, SessionStatus.Active);
        session.AddDomainEvent(new SessionStarted(session.Id, session.UserId, nowUtc));
        return session;
    }

    /// <summary>
    /// Records activity, extending <see cref="ExpiresAtUtc"/> by <see cref="IdleTimeout"/> from now.
    /// Routine bookkeeping - deliberately does not raise a domain event on every call.
    /// </summary>
    /// <exception cref="AuthenticationDomainException">The session is not active.</exception>
    public void Touch(DateTimeOffset nowUtc)
    {
        if (Status != SessionStatus.Active)
            throw AuthenticationDomainException.SessionNotActive(Id);

        LastActivityAtUtc = nowUtc;
        ExpiresAtUtc = nowUtc + IdleTimeout;
    }

    /// <summary>Transitions the session to expired because its idle timeout elapsed.</summary>
    /// <exception cref="AuthenticationDomainException">The session is not active, or its expiry instant has not yet been reached.</exception>
    public void Expire(DateTimeOffset nowUtc)
    {
        if (Status != SessionStatus.Active)
            throw AuthenticationDomainException.SessionNotActive(Id);
        if (nowUtc < ExpiresAtUtc)
            throw AuthenticationDomainException.SessionNotYetExpired(Id);

        Status = SessionStatus.Expired;
        AddDomainEvent(new SessionExpired(Id, nowUtc));
    }

    /// <summary>Administratively terminates the session (e.g. security action, account lockout).</summary>
    /// <exception cref="AuthenticationDomainException">The session is not active.</exception>
    public void Revoke(DateTimeOffset nowUtc)
    {
        if (Status != SessionStatus.Active)
            throw AuthenticationDomainException.SessionNotActive(Id);

        Status = SessionStatus.Revoked;
        AddDomainEvent(new SessionRevoked(Id, nowUtc));
    }

    /// <summary>Ends the session via the user's own explicit sign-out.</summary>
    /// <exception cref="AuthenticationDomainException">The session is not active.</exception>
    public void LogOut(DateTimeOffset nowUtc)
    {
        if (Status != SessionStatus.Active)
            throw AuthenticationDomainException.SessionNotActive(Id);

        Status = SessionStatus.LoggedOut;
        AddDomainEvent(new SessionLoggedOut(Id, nowUtc));
    }
}
