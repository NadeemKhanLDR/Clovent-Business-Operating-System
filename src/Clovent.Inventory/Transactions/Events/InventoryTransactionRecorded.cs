using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Transactions.Events;

/// <summary>Raised when a new <see cref="InventoryTransaction"/> ledger entry is recorded.</summary>
public sealed record InventoryTransactionRecorded(
    InventoryTransactionId InventoryTransactionId,
    WarehouseId WarehouseId,
    ProductVariantId ProductVariantId,
    InventoryTransactionType TransactionType,
    decimal Quantity,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
