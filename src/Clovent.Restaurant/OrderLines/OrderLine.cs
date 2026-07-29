using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.Restaurant.OrderLines.Events;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.OrderLines;

/// <summary>
/// One line of an <see cref="Orders.Order"/> - a quantity of one
/// <see cref="ProductVariantId"/> at a price/tax rate snapshotted at the
/// moment it was added, so a later catalog price or tax-rate change never
/// retroactively alters an order already in progress or completed. A
/// separate aggregate from <see cref="Orders.Order"/> (not a child entity
/// embedded in it) so it can be reassigned to a different order
/// (<see cref="TransferToOrder"/>) for Table Split/Merge without touching
/// the order aggregates themselves beyond updating their own id lists - see
/// <c>OrderLifecycle.md</c>.
/// </summary>
public sealed class OrderLine : AggregateRoot<OrderLineId>
{
    /// <summary>The order this line currently belongs to - mutable via <see cref="TransferToOrder"/> for Table Split/Merge.</summary>
    public OrderId OrderId { get; private set; }

    /// <summary>The variant being ordered, fixed at creation.</summary>
    public ProductVariantId ProductVariantId { get; }

    /// <summary>The quantity ordered.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>The unit price, snapshotted at creation from the variant's active selling price.</summary>
    public decimal UnitPrice { get; }

    /// <summary>The tax rate percentage, snapshotted at creation from the product's tax configuration.</summary>
    public decimal TaxRatePercentage { get; }

    /// <summary>Whether <see cref="UnitPrice"/> already includes tax, snapshotted at creation.</summary>
    public bool TaxIsInclusive { get; }

    /// <summary>Free-text item notes (e.g. "no onions"), if any.</summary>
    public string? Notes { get; private set; }

    /// <summary>Whether this line has been voided - excluded from totals and kitchen tickets, but never physically removed once created.</summary>
    public bool IsVoided { get; private set; }

    /// <summary>UTC instant this line was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>This line's total before tax/discounts: <see cref="Quantity"/> times <see cref="UnitPrice"/>.</summary>
    public decimal LineTotal => Quantity * UnitPrice;

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private OrderLine(
        OrderLineId id,
        OrderId orderId,
        ProductVariantId productVariantId,
        decimal quantity,
        decimal unitPrice,
        decimal taxRatePercentage,
        bool taxIsInclusive,
        string? notes,
        bool isVoided,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrderId = orderId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRatePercentage = taxRatePercentage;
        TaxIsInclusive = taxIsInclusive;
        Notes = notes;
        IsVoided = isVoided;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new order line.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive, or <paramref name="unitPrice"/> is negative.</exception>
    public static OrderLine Create(
        OrderId orderId,
        ProductVariantId productVariantId,
        decimal quantity,
        decimal unitPrice,
        decimal taxRatePercentage,
        bool taxIsInclusive,
        string? notes = null)
    {
        RequirePositiveQuantity(quantity);
        RequireNonNegativePrice(unitPrice);

        var now = DateTimeOffset.UtcNow;
        var line = new OrderLine(OrderLineId.New(), orderId, productVariantId, quantity, unitPrice, taxRatePercentage, taxIsInclusive, notes, false, now);
        line.AddDomainEvent(new OrderLineCreated(line.Id, line.OrderId, line.ProductVariantId, line.Quantity, line.UnitPrice, now));
        return line;
    }

    /// <summary>Changes the ordered quantity.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive.</exception>
    public void SetQuantity(decimal quantity)
    {
        RequirePositiveQuantity(quantity);
        if (Quantity == quantity) return;

        Quantity = quantity;
        AddDomainEvent(new OrderLineQuantityChanged(Id, quantity, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets this line's item notes. A no-op (no event raised) if unchanged.</summary>
    public void SetNotes(string? notes)
    {
        if (Notes == notes) return;

        Notes = notes;
        AddDomainEvent(new OrderLineNotesChanged(Id, notes, DateTimeOffset.UtcNow));
    }

    /// <summary>Voids this line - excludes it from totals and future kitchen tickets.</summary>
    /// <exception cref="RestaurantDomainException">The line is already voided.</exception>
    public void Void()
    {
        if (IsVoided)
            throw RestaurantDomainException.OrderLineAlreadyVoided(Id);

        IsVoided = true;
        AddDomainEvent(new OrderLineVoided(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Restores a voided line.</summary>
    /// <exception cref="RestaurantDomainException">The line is not voided.</exception>
    public void Unvoid()
    {
        if (!IsVoided)
            throw RestaurantDomainException.OrderLineNotVoided(Id);

        IsVoided = false;
        AddDomainEvent(new OrderLineUnvoided(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Moves this line to a different order - the mechanism behind Table Split/Merge.</summary>
    public void TransferToOrder(OrderId orderId)
    {
        if (OrderId == orderId) return;

        OrderId = orderId;
        AddDomainEvent(new OrderLineTransferredToOrder(Id, orderId, DateTimeOffset.UtcNow));
    }

    private static void RequirePositiveQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");
    }

    private static void RequireNonNegativePrice(decimal unitPrice)
    {
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Unit price cannot be negative.");
    }
}
