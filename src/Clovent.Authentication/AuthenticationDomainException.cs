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

    /// <summary>A candidate password failed <see cref="Passwords.PasswordPolicy"/> evaluation.</summary>
    public static AuthenticationDomainException PasswordPolicyViolated(IReadOnlyList<string> violations) =>
        new(string.Join(" ", violations));

    /// <summary>A self-service password change supplied a current password that does not match the stored hash.</summary>
    public static AuthenticationDomainException CurrentPasswordIncorrect() =>
        new("The current password is incorrect.");

    /// <summary>A candidate PIN failed <see cref="Pins.PinPolicy"/> evaluation.</summary>
    public static AuthenticationDomainException PinPolicyViolated(IReadOnlyList<string> violations) =>
        new(string.Join(" ", violations));

    /// <summary>
    /// A candidate PIN is already in use by another user. PINs are unique
    /// across users because PIN-only sign-in resolves the user FROM the PIN -
    /// a duplicate would make authentication ambiguous.
    /// </summary>
    public static AuthenticationDomainException PinAlreadyInUse() =>
        new("This PIN is already in use by another user. Choose a different PIN.");
}
