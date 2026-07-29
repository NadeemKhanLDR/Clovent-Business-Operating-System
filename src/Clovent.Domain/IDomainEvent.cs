namespace Clovent.Domain;

/// <summary>
/// Marker for something business-significant that happened to an aggregate.
/// Deliberately has no dependency on any messaging/mediator library - the
/// domain layer only models that the event occurred, not how it is
/// dispatched. Wiring an event to a bus/mediator is an Application or
/// Infrastructure concern for a later milestone.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC instant the event occurred.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}
