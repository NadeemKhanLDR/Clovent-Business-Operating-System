using Clovent.Domain;
using Clovent.Restaurant.Discounts.Events;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Discounts;

/// <summary>
/// A discount applied to one <see cref="Orders.Order"/> - an applied
/// instance, not a reusable discount-code catalog entry (no separate
/// aggregate for "discount definitions" exists in this milestone's scope).
/// Immutable once created: correcting a wrong discount means removing it
/// from the order (<see cref="Order.RemoveDiscount"/>) and applying a new
/// one, not editing this one in place.
/// </summary>
public sealed class Discount : AggregateRoot<DiscountId>
{
    /// <summary>The order this discount applies to, fixed at creation.</summary>
    public OrderId OrderId { get; }

    /// <summary>Whether <see cref="Value"/> is a percentage or a fixed amount, fixed at creation.</summary>
    public DiscountType DiscountType { get; }

    /// <summary>The discount's magnitude - a 0-100 percentage or a currency amount, depending on <see cref="DiscountType"/>.</summary>
    public decimal Value { get; }

    /// <summary>Why this discount was applied.</summary>
    public string Reason { get; }

    /// <summary>UTC instant this discount was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Discount(DiscountId id, OrderId orderId, DiscountType discountType, decimal value, string reason, DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrderId = orderId;
        DiscountType = discountType;
        Value = value;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new discount for the given order.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative, or a <see cref="DiscountType.Percentage"/> greater than 100.</exception>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is empty.</exception>
    public static Discount Create(OrderId orderId, DiscountType discountType, decimal value, string reason)
    {
        RequireValidValue(discountType, value);
        reason = RequireReason(reason);

        var now = DateTimeOffset.UtcNow;
        var discount = new Discount(DiscountId.New(), orderId, discountType, value, reason, now);
        discount.AddDomainEvent(new DiscountCreated(discount.Id, discount.OrderId, discount.DiscountType, discount.Value, now));
        return discount;
    }

    private static void RequireValidValue(DiscountType discountType, decimal value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Discount value cannot be negative.");

        if (discountType == DiscountType.Percentage && value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Percentage discount cannot exceed 100.");
    }

    private static string RequireReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Discount reason is required.", nameof(value));

        return value.Trim();
    }
}
