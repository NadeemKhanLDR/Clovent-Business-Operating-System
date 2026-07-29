using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Clovent.Domain;

namespace Clovent.Authentication;

/// <summary>
/// Raised when an Authentication Domain aggregate operation would violate one
/// of its invariants. Mirrors <c>Clovent.Identity.IdentityDomainException</c>:
/// a single type with named factory methods (one per rule) rather than a
/// subclass per rule.
/// </summary>
public sealed class AuthenticationDomainException : DomainException
{
    private AuthenticationDomainException(string message) : base(message)
    {
    }

    /// <summary>A session operation requiring <see cref="SessionStatus.Active"/> was attempted while it was not.</summary>
    public static AuthenticationDomainException SessionNotActive(SessionId sessionId) =>
        new($"Session '{sessionId}' is not active.");

    /// <summary>Session Expire() was attempted before its expiry instant was reached.</summary>
    public static AuthenticationDomainException SessionNotYetExpired(SessionId sessionId) =>
        new($"Session '{sessionId}' has not yet reached its expiry time.");

    /// <summary>A refresh session operation requiring <see cref="RefreshSessionStatus.Active"/> was attempted while it was not.</summary>
    public static AuthenticationDomainException RefreshSessionNotActive(RefreshSessionId refreshSessionId) =>
        new($"Refresh session '{refreshSessionId}' is not active.");

    /// <summary>RefreshSession Expire() was attempted before its expiry instant was reached.</summary>
    public static AuthenticationDomainException RefreshSessionNotYetExpired(RefreshSessionId refreshSessionId) =>
        new($"Refresh session '{refreshSessionId}' has not yet reached its expiry time.");

    /// <summary>RefreshSession Rotate() was attempted after its expiry instant was reached.</summary>
    public static AuthenticationDomainException RefreshSessionExpired(RefreshSessionId refreshSessionId) =>
        new($"Refresh session '{refreshSessionId}' has expired and cannot be rotated.");
}
