using Clovent.Domain;

namespace Clovent.Authentication.RefreshSessions.Events;

/// <summary>Raised when a <see cref="RefreshSession"/> is superseded by a newly-issued one.</summary>
public sealed record RefreshSessionRotated(RefreshSessionId RefreshSessionId, RefreshSessionId ReplacedByRefreshSessionId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
