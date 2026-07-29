using Clovent.Domain;
using Clovent.Identity.Users.ValueObjects;

namespace Clovent.Identity.Users.Events;

/// <summary>Raised when a new <see cref="User"/> is created, in <see cref="UserStatus.PendingActivation"/>.</summary>
public sealed record UserCreated(UserId UserId, Email Email, UserName UserName, DateTimeOffset OccurredOnUtc) : IDomainEvent;
