namespace Clovent.Restaurant.Discounts;

/// <summary>Strongly-typed identifier for a <see cref="Discount"/> aggregate.</summary>
public readonly record struct DiscountId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("DiscountId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="DiscountId"/>.</summary>
    public static DiscountId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
