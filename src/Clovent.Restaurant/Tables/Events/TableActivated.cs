using Clovent.Domain;

namespace Clovent.Restaurant.Tables.Events;

/// <summary>Raised when a <see cref="Table"/> is (re)activated.</summary>
public sealed record TableActivated(TableId TableId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
