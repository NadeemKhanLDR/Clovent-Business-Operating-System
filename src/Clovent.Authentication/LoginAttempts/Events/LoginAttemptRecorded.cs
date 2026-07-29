using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Authentication.LoginAttempts.Events;

/// <summary>Raised when a <see cref="LoginAttempt"/> is recorded, whatever its outcome.</summary>
public sealed record LoginAttemptRecorded(
    LoginAttemptId LoginAttemptId,
    UserId? UserId,
    LoginOutcome Outcome,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
