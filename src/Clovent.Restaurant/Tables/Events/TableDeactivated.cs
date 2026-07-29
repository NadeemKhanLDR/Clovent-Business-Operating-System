using Clovent.Domain;

namespace Clovent.Restaurant.Tables.Events;

/// <summary>Raised when a <see cref="Table"/> is deactivated.</summary>
public sealed record TableDeactivated(TableId TableId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
