using Clovent.Authentication.Credentials.Events;
using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Authentication.Credentials;

/// <summary>
/// Owns a user's credential state - the <see cref="UserId"/> plus the six
/// concepts <c>AuthenticationDomain.md</c> Section 8 modeled as standalone
/// value types (<see cref="PasswordHash"/>, <see cref="PinHash"/>,
/// <see cref="SecurityStamp"/>, <see cref="PasswordHistory"/> (which derives
/// "last changed"), <see cref="FailedAttempts"/>) with no owner until now.
/// This is deliberately a thin aggregate: it holds these values and applies
/// the one rule that spans more than one of them at a time (changing a
/// password or PIN rotates <see cref="SecurityStamp"/>, since both are
/// security-relevant events that must invalidate anything that cached the
/// old stamp) - it does not hash, validate policy, or evaluate lockouts;
/// those remain <see cref="Passwords.PasswordPolicy"/>, <see cref="Pins.PinPolicy"/>,
/// and <see cref="Lockouts.LockoutPolicy"/>'s jobs respectively.
/// </summary>
public sealed class UserCredentials : AggregateRoot<UserCredentialsId>
{
    /// <summary>The user this credential record belongs to.</summary>
    public UserId UserId { get; }

    /// <summary>The user's current password hash, or <see langword="null"/> if no password has been set.</summary>
    public PasswordHash? PasswordHash { get; private set; }

    /// <summary>The user's current PIN hash, or <see langword="null"/> if no PIN has been set.</summary>
    public PinHash? PinHash { get; private set; }

    /// <summary>The current security stamp - changes on every password/PIN change.</summary>
    public SecurityStamp SecurityStamp { get; private set; }

    /// <summary>The trailing history of previously-used password hashes.</summary>
    public PasswordHistory PasswordHistory { get; private set; }

    /// <summary>The consecutive-failure counter, incremented/reset by the Application layer as login attempts are recorded.</summary>
    public FailedAttempts FailedAttempts { get; private set; }

    /// <summary>
    /// Takes every persisted field explicitly so this is the single,
    /// unambiguous constructor an EF Core Infrastructure implementation can
    /// bind to when materializing an existing credential record from storage.
    /// </summary>
    private UserCredentials(
        UserCredentialsId id,
        UserId userId,
        PasswordHash? passwordHash,
        PinHash? pinHash,
        SecurityStamp securityStamp,
        PasswordHistory passwordHistory,
        FailedAttempts failedAttempts)
    {
        Id = id;
        UserId = userId;
        PasswordHash = passwordHash;
        PinHash = pinHash;
        SecurityStamp = securityStamp;
        PasswordHistory = passwordHistory;
        FailedAttempts = failedAttempts;
    }

    /// <summary>Creates a new, empty credential record for a user - no password or PIN set yet.</summary>
    public static UserCredentials Create(UserId userId, DateTimeOffset nowUtc)
    {
        var credentials = new UserCredentials(
            UserCredentialsId.New(),
            userId,
            passwordHash: null,
            pinHash: null,
            SecurityStamp.Generate(),
            PasswordHistory.Empty,
            FailedAttempts.Zero);

        credentials.AddDomainEvent(new UserCredentialsCreated(credentials.Id, credentials.UserId, nowUtc));
        return credentials;
    }

    /// <summary>
    /// Sets a new password hash, records it in <see cref="PasswordHistory"/>,
    /// and rotates <see cref="SecurityStamp"/> since a password change is a
    /// security-relevant event.
    /// </summary>
    public void SetPassword(PasswordHash hash, DateTimeOffset nowUtc, int maxHistorySize = PasswordHistory.DefaultMaxSize)
    {
        ArgumentNullException.ThrowIfNull(hash);

        PasswordHash = hash;
        PasswordHistory = PasswordHistory.WithNewPassword(hash, nowUtc, maxHistorySize);
        SecurityStamp = SecurityStamp.Generate();
        AddDomainEvent(new PasswordChanged(Id, UserId, nowUtc));
    }

    /// <summary>Sets a new PIN hash and rotates <see cref="SecurityStamp"/> since a PIN change is a security-relevant event.</summary>
    public void SetPin(PinHash hash, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(hash);

        PinHash = hash;
        SecurityStamp = SecurityStamp.Generate();
        AddDomainEvent(new PinChanged(Id, UserId, nowUtc));
    }

    /// <summary>
    /// Records another consecutive login failure. Routine bookkeeping - like
    /// <c>Session.Touch</c>, deliberately does not raise a domain event.
    /// </summary>
    public void RecordFailedAttempt() => FailedAttempts = FailedAttempts.Increment();

    /// <summary>Resets the consecutive-failure counter, e.g. after a successful authentication.</summary>
    public void ResetFailedAttempts() => FailedAttempts = FailedAttempts.Reset();
}
