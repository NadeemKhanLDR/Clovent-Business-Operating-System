using Clovent.Domain;

namespace Clovent.Identity.Users.Events;

/// <summary>Raised when an active <see cref="User"/> is deliberately deactivated.</summary>
public sealed record UserDeactivated(UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
