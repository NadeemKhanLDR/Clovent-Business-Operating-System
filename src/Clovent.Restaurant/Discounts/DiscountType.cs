namespace Clovent.Restaurant.Discounts;

/// <summary>How a <see cref="Discount"/>'s <see cref="Discount.Value"/> is interpreted.</summary>
public enum DiscountType
{
    /// <summary>A percentage (0-100) of the order subtotal.</summary>
    Percentage,

    /// <summary>A fixed currency amount.</summary>
    FixedAmount
}
