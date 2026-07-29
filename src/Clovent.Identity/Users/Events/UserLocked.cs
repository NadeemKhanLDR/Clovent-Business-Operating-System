using Clovent.Domain;

namespace Clovent.Identity.Users.Events;

/// <summary>Raised when a <see cref="User"/> is locked out.</summary>
public sealed record UserLocked(UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
