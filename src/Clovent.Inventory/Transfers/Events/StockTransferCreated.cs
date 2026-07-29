using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Transfers.Events;

/// <summary>Raised when a new <see cref="StockTransfer"/> is proposed.</summary>
public sealed record StockTransferCreated(
    StockTransferId StockTransferId,
    WarehouseId SourceWarehouseId,
    WarehouseId DestinationWarehouseId,
    ProductVariantId ProductVariantId,
    decimal Quantity,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
