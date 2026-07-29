using Clovent.Domain;

namespace Clovent.Authentication.RefreshSessions.Events;

/// <summary>Raised when a <see cref="RefreshSession"/> transitions to expired because its expiry instant was reached.</summary>
public sealed record RefreshSessionExpired(RefreshSessionId RefreshSessionId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
