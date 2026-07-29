using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.Inventory.Transactions.Events;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Transactions;

/// <summary>
/// An immutable, append-only ledger entry recording one stock movement -
/// the audit trail behind every <see cref="WarehouseStocks.WarehouseStock"/>
/// mutation. Created by the Application-layer handler that also mutates the
/// corresponding <c>WarehouseStock</c> (never by the stock aggregate
/// itself), and has no update/delete behavior once recorded - a ledger
/// entry is never edited, only ever superseded by a later one.
/// </summary>
public sealed class InventoryTransaction : AggregateRoot<InventoryTransactionId>
{
    /// <summary>The warehouse this movement occurred at.</summary>
    public WarehouseId WarehouseId { get; }

    /// <summary>The variant this movement affected.</summary>
    public ProductVariantId ProductVariantId { get; }

    /// <summary>What kind of movement this is.</summary>
    public InventoryTransactionType TransactionType { get; }

    /// <summary>The quantity moved - always positive; direction is implied by <see cref="TransactionType"/>.</summary>
    public decimal Quantity { get; }

    /// <summary>The kind of record this movement traces back to (e.g. <c>"StockAdjustment"</c>, <c>"StockTransfer"</c>), if any.</summary>
    public string? ReferenceType { get; }

    /// <summary>The identity of the record this movement traces back to, if any.</summary>
    public Guid? ReferenceId { get; }

    /// <summary>Free-text context for this movement, if any.</summary>
    public string? Notes { get; }

    /// <summary>UTC instant this movement occurred.</summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private InventoryTransaction(
        InventoryTransactionId id,
        WarehouseId warehouseId,
        ProductVariantId productVariantId,
        InventoryTransactionType transactionType,
        decimal quantity,
        string? referenceType,
        Guid? referenceId,
        string? notes,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        WarehouseId = warehouseId;
        ProductVariantId = productVariantId;
        TransactionType = transactionType;
        Quantity = quantity;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Notes = notes;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>Records a new inventory movement.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive.</exception>
    public static InventoryTransaction Create(
        WarehouseId warehouseId,
        ProductVariantId productVariantId,
        InventoryTransactionType transactionType,
        decimal quantity,
        string? referenceType = null,
        Guid? referenceId = null,
        string? notes = null,
        DateTimeOffset? occurredAtUtc = null)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");

        var now = occurredAtUtc ?? DateTimeOffset.UtcNow;
        var transaction = new InventoryTransaction(InventoryTransactionId.New(), warehouseId, productVariantId, transactionType, quantity, referenceType, referenceId, notes, now);
        transaction.AddDomainEvent(new InventoryTransactionRecorded(transaction.Id, transaction.WarehouseId, transaction.ProductVariantId, transaction.TransactionType, transaction.Quantity, now));
        return transaction;
    }
}
