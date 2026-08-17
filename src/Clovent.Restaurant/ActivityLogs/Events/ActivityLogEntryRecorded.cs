using Clovent.Domain;

namespace Clovent.Restaurant.ActivityLogs.Events;

/// <summary>Raised when a new <see cref="ActivityLogEntry"/> is recorded.</summary>
public sealed record ActivityLogEntryRecorded(ActivityLogEntryId ActivityLogEntryId, string Action, DateTimeOffset OccurredOnUtc) : IDomainEvent;
