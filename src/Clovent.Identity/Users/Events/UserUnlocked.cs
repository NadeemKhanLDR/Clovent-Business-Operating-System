using Clovent.Domain;

namespace Clovent.Identity.Users.Events;

/// <summary>Raised when a locked <see cref="User"/> is unlocked, returning it to <see cref="UserStatus.Active"/>.</summary>
public sealed record UserUnlocked(UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
