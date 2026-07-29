using Clovent.Authentication.LoginAttempts.Events;
using Clovent.Authentication.Shared.ValueObjects;
using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Authentication.LoginAttempts;

/// <summary>
/// An immutable audit record of a single login attempt, successful or not.
/// <see cref="AttemptedIdentifier"/> is deliberately a raw string rather
/// than a validated <c>UserName</c>/<c>Email</c> value object - an attempt
/// must be recordable even when the identifier submitted is malformed or
/// matches no user at all, which is exactly the case a value object's
/// factory would reject.
/// </summary>
public sealed class LoginAttempt : AggregateRoot<LoginAttemptId>
{
    private const int MaxIdentifierLength = 320;

    /// <summary>The raw identifier (username or email) submitted for this attempt.</summary>
    public string AttemptedIdentifier { get; }

    /// <summary>The matched user, if the identifier resolved to one - regardless of whether the attempt then succeeded.</summary>
    public UserId? UserId { get; }

    /// <summary>The result of this attempt.</summary>
    public LoginOutcome Outcome { get; }

    /// <summary>The IP address the attempt originated from, if known.</summary>
    public IpAddress? IpAddress { get; }

    /// <summary>UTC instant the attempt occurred.</summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary><see langword="true"/> for any outcome other than <see cref="LoginOutcome.Succeeded"/>.</summary>
    public bool IsFailure => Outcome != LoginOutcome.Succeeded;

    private LoginAttempt(
        LoginAttemptId id,
        string attemptedIdentifier,
        UserId? userId,
        LoginOutcome outcome,
        IpAddress? ipAddress,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        AttemptedIdentifier = attemptedIdentifier;
        UserId = userId;
        Outcome = outcome;
        IpAddress = ipAddress;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>Records a completed login attempt and its outcome.</summary>
    /// <exception cref="ArgumentException"><paramref name="attemptedIdentifier"/> is empty or exceeds <c>320</c> characters.</exception>
    public static LoginAttempt Record(
        string attemptedIdentifier,
        UserId? userId,
        LoginOutcome outcome,
        DateTimeOffset nowUtc,
        IpAddress? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(attemptedIdentifier))
            throw new ArgumentException("Attempted identifier is required.", nameof(attemptedIdentifier));

        attemptedIdentifier = attemptedIdentifier.Trim();

        if (attemptedIdentifier.Length > MaxIdentifierLength)
            throw new ArgumentException($"Attempted identifier cannot exceed {MaxIdentifierLength} characters.", nameof(attemptedIdentifier));

        var attempt = new LoginAttempt(LoginAttemptId.New(), attemptedIdentifier, userId, outcome, ipAddress, nowUtc);
        attempt.AddDomainEvent(new LoginAttemptRecorded(attempt.Id, attempt.UserId, attempt.Outcome, nowUtc));
        return attempt;
    }
}
