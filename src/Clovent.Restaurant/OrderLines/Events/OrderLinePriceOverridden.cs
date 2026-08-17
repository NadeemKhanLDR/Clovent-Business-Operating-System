using Clovent.Domain;

namespace Clovent.Restaurant.OrderLines.Events;

/// <summary>Raised when a cashier overrides an <see cref="OrderLine"/>'s unit price away from its catalog-snapshotted value.</summary>
public sealed record OrderLinePriceOverridden(
    OrderLineId OrderLineId,
    decimal OriginalUnitPrice,
    decimal NewUnitPrice,
    string Reason,
    string PerformedBy,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
