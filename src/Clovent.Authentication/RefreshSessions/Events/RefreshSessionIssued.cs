using Clovent.Authentication.Sessions;
using Clovent.Domain;

namespace Clovent.Authentication.RefreshSessions.Events;

/// <summary>Raised when a new <see cref="RefreshSession"/> is issued for a <see cref="Session"/>.</summary>
public sealed record RefreshSessionIssued(RefreshSessionId RefreshSessionId, SessionId SessionId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
