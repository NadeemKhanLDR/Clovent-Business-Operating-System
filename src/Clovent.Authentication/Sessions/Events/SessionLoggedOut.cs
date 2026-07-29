using Clovent.Domain;

namespace Clovent.Authentication.Sessions.Events;

/// <summary>Raised when a <see cref="Session"/> is ended by the user's own explicit sign-out.</summary>
public sealed record SessionLoggedOut(SessionId SessionId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
