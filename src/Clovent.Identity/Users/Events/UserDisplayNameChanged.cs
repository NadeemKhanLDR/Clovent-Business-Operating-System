using Clovent.Domain;
using Clovent.Identity.Users.ValueObjects;

namespace Clovent.Identity.Users.Events;

/// <summary>Raised when a <see cref="User"/>'s <see cref="DisplayName"/> changes.</summary>
public sealed record UserDisplayNameChanged(UserId UserId, DisplayName NewDisplayName, DateTimeOffset OccurredOnUtc) : IDomainEvent;
