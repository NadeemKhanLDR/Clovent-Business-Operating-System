using Clovent.Domain;

namespace Clovent.Identity.Users.Events;

/// <summary>Raised when a <see cref="User"/> transitions into <see cref="UserStatus.Active"/>.</summary>
public sealed record UserActivated(UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
