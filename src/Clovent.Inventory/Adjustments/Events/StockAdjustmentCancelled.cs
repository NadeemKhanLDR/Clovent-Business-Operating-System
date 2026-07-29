using Clovent.Domain;

namespace Clovent.Inventory.Adjustments.Events;

/// <summary>Raised when a pending <see cref="StockAdjustment"/> is cancelled.</summary>
public sealed record StockAdjustmentCancelled(StockAdjustmentId StockAdjustmentId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
