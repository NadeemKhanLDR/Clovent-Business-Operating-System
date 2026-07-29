using Clovent.Domain;

namespace Clovent.Authentication.Sessions.Events;

/// <summary>Raised when a <see cref="Session"/> is administratively revoked.</summary>
public sealed record SessionRevoked(SessionId SessionId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
