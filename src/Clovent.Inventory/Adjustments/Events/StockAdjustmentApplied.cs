using Clovent.Domain;

namespace Clovent.Inventory.Adjustments.Events;

/// <summary>Raised when a <see cref="StockAdjustment"/> is applied to warehouse stock.</summary>
public sealed record StockAdjustmentApplied(StockAdjustmentId StockAdjustmentId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
