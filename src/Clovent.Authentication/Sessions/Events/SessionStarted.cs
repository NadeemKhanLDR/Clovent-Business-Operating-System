using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Authentication.Sessions.Events;

/// <summary>Raised when a new <see cref="Session"/> is started for a user.</summary>
public sealed record SessionStarted(SessionId SessionId, UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
