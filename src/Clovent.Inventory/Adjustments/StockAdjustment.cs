using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.Inventory.Adjustments.Events;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Adjustments;

/// <summary>
/// A proposed correction to a warehouse's stock balance (e.g. after a
/// physical count finds a discrepancy) - created <see cref="StockAdjustmentStatus.Pending"/>,
/// then either <see cref="Apply"/>'d (mutating the corresponding
/// <see cref="WarehouseStocks.WarehouseStock"/> and recording an
/// <see cref="Transactions.InventoryTransaction"/> - both the Application
/// handler's job, not this aggregate's) or <see cref="Cancel"/>'d. Applying
/// is a one-way transition, mirroring <c>FiscalYear.Close()</c>'s identical
/// "no undo" reasoning - a correction that turns out to be wrong is
/// reversed by a second, opposite adjustment, not by un-applying the first.
/// </summary>
public sealed class StockAdjustment : AggregateRoot<StockAdjustmentId>
{
    private const int MaxReasonLength = 500;

    /// <summary>The warehouse this adjustment affects, fixed at creation.</summary>
    public WarehouseId WarehouseId { get; }

    /// <summary>The variant this adjustment affects, fixed at creation.</summary>
    public ProductVariantId ProductVariantId { get; }

    /// <summary>Whether this increases or decreases quantity on hand, fixed at creation.</summary>
    public StockAdjustmentType AdjustmentType { get; }

    /// <summary>The quantity to adjust by, fixed at creation.</summary>
    public decimal Quantity { get; }

    /// <summary>Why this adjustment was proposed, fixed at creation.</summary>
    public string Reason { get; }

    /// <summary>The adjustment's current workflow state.</summary>
    public StockAdjustmentStatus Status { get; private set; }

    /// <summary>UTC instant this adjustment was proposed.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant this adjustment was applied, if it has been.</summary>
    public DateTimeOffset? AppliedAtUtc { get; private set; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private StockAdjustment(
        StockAdjustmentId id,
        WarehouseId warehouseId,
        ProductVariantId productVariantId,
        StockAdjustmentType adjustmentType,
        decimal quantity,
        string reason,
        StockAdjustmentStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? appliedAtUtc)
    {
        Id = id;
        WarehouseId = warehouseId;
        ProductVariantId = productVariantId;
        AdjustmentType = adjustmentType;
        Quantity = quantity;
        Reason = reason;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        AppliedAtUtc = appliedAtUtc;
    }

    /// <summary>Proposes a new, pending stock adjustment.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is empty or too long.</exception>
    public static StockAdjustment Create(WarehouseId warehouseId, ProductVariantId productVariantId, StockAdjustmentType adjustmentType, decimal quantity, string reason)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");

        reason = RequireReason(reason);

        var now = DateTimeOffset.UtcNow;
        var adjustment = new StockAdjustment(StockAdjustmentId.New(), warehouseId, productVariantId, adjustmentType, quantity, reason, StockAdjustmentStatus.Pending, now, null);
        adjustment.AddDomainEvent(new StockAdjustmentCreated(adjustment.Id, adjustment.WarehouseId, adjustment.ProductVariantId, adjustment.AdjustmentType, adjustment.Quantity, now));
        return adjustment;
    }

    /// <summary>Applies the adjustment. A one-way transition - there is no Unapply().</summary>
    /// <exception cref="InventoryDomainException">The adjustment is not <see cref="StockAdjustmentStatus.Pending"/>.</exception>
    public void Apply()
    {
        if (Status != StockAdjustmentStatus.Pending)
            throw InventoryDomainException.StockAdjustmentNotPending(Id);

        Status = StockAdjustmentStatus.Applied;
        AppliedAtUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new StockAdjustmentApplied(Id, AppliedAtUtc.Value));
    }

    /// <summary>Cancels the adjustment before it is applied.</summary>
    /// <exception cref="InventoryDomainException">The adjustment is not <see cref="StockAdjustmentStatus.Pending"/>.</exception>
    public void Cancel()
    {
        if (Status != StockAdjustmentStatus.Pending)
            throw InventoryDomainException.StockAdjustmentNotPending(Id);

        Status = StockAdjustmentStatus.Cancelled;
        AddDomainEvent(new StockAdjustmentCancelled(Id, DateTimeOffset.UtcNow));
    }

    private static string RequireReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Adjustment reason is required.", nameof(value));

        value = value.Trim();

        if (value.Length > MaxReasonLength)
            throw new ArgumentException($"Adjustment reason cannot exceed {MaxReasonLength} characters.", nameof(value));

        return value;
    }
}
