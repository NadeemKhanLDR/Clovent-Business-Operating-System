using Clovent.Domain;

namespace Clovent.Authentication.RefreshSessions.Events;

/// <summary>Raised when a <see cref="RefreshSession"/> is administratively revoked.</summary>
public sealed record RefreshSessionRevoked(RefreshSessionId RefreshSessionId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
