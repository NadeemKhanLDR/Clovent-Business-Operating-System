using Clovent.Domain;

namespace Clovent.Inventory.Transfers.Events;

/// <summary>Raised when a pending <see cref="StockTransfer"/> is cancelled.</summary>
public sealed record StockTransferCancelled(StockTransferId StockTransferId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
