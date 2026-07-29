using Clovent.Domain;

namespace Clovent.Inventory.Transfers.Events;

/// <summary>Raised when a <see cref="StockTransfer"/> is completed.</summary>
public sealed record StockTransferCompleted(StockTransferId StockTransferId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
