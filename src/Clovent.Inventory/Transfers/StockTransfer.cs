using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.Inventory.Transfers.Events;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Transfers;

/// <summary>
/// A proposed movement of stock from one warehouse to another - created
/// <see cref="StockTransferStatus.Pending"/>, then either
/// <see cref="Complete"/>'d (issuing from the source and receiving at the
/// destination, plus recording two <see cref="Transactions.InventoryTransaction"/>
/// entries - all the Application handler's job, not this aggregate's) or
/// <see cref="Cancel"/>'d. Completing is one-way, mirroring
/// <c>StockAdjustment.Apply</c>'s identical reasoning.
/// </summary>
public sealed class StockTransfer : AggregateRoot<StockTransferId>
{
    /// <summary>The warehouse stock leaves, fixed at creation.</summary>
    public WarehouseId SourceWarehouseId { get; }

    /// <summary>The warehouse stock arrives at, fixed at creation.</summary>
    public WarehouseId DestinationWarehouseId { get; }

    /// <summary>The variant being transferred, fixed at creation.</summary>
    public ProductVariantId ProductVariantId { get; }

    /// <summary>The quantity to transfer, fixed at creation.</summary>
    public decimal Quantity { get; }

    /// <summary>The transfer's current workflow state.</summary>
    public StockTransferStatus Status { get; private set; }

    /// <summary>UTC instant this transfer was proposed.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant this transfer was completed, if it has been.</summary>
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private StockTransfer(
        StockTransferId id,
        WarehouseId sourceWarehouseId,
        WarehouseId destinationWarehouseId,
        ProductVariantId productVariantId,
        decimal quantity,
        StockTransferStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        Id = id;
        SourceWarehouseId = sourceWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    /// <summary>Proposes a new, pending stock transfer.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive.</exception>
    /// <exception cref="InventoryDomainException"><paramref name="sourceWarehouseId"/> equals <paramref name="destinationWarehouseId"/>.</exception>
    public static StockTransfer Create(WarehouseId sourceWarehouseId, WarehouseId destinationWarehouseId, ProductVariantId productVariantId, decimal quantity)
    {
        if (sourceWarehouseId.Equals(destinationWarehouseId))
            throw InventoryDomainException.TransferSourceEqualsDestination(sourceWarehouseId);

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");

        var now = DateTimeOffset.UtcNow;
        var transfer = new StockTransfer(StockTransferId.New(), sourceWarehouseId, destinationWarehouseId, productVariantId, quantity, StockTransferStatus.Pending, now, null);
        transfer.AddDomainEvent(new StockTransferCreated(transfer.Id, transfer.SourceWarehouseId, transfer.DestinationWarehouseId, transfer.ProductVariantId, transfer.Quantity, now));
        return transfer;
    }

    /// <summary>Completes the transfer. A one-way transition - there is no Uncomplete().</summary>
    /// <exception cref="InventoryDomainException">The transfer is not <see cref="StockTransferStatus.Pending"/>.</exception>
    public void Complete()
    {
        if (Status != StockTransferStatus.Pending)
            throw InventoryDomainException.StockTransferNotPending(Id);

        Status = StockTransferStatus.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new StockTransferCompleted(Id, CompletedAtUtc.Value));
    }

    /// <summary>Cancels the transfer before it is completed.</summary>
    /// <exception cref="InventoryDomainException">The transfer is not <see cref="StockTransferStatus.Pending"/>.</exception>
    public void Cancel()
    {
        if (Status != StockTransferStatus.Pending)
            throw InventoryDomainException.StockTransferNotPending(Id);

        Status = StockTransferStatus.Cancelled;
        AddDomainEvent(new StockTransferCancelled(Id, DateTimeOffset.UtcNow));
    }
}
