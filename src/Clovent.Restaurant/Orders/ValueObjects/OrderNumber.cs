using Clovent.Domain;

namespace Clovent.Restaurant.Orders.ValueObjects;

/// <summary>
/// An order's human-facing display number (e.g. for a receipt or the
/// Running Orders screen) - distinct from its <see cref="OrderId"/>, the
/// same "SKU vs system id" separation <c>Clovent.Catalog.Shared.ValueObjects.Sku</c>'s
/// doc comment already establishes for a different aggregate. Generated
/// from a UTC timestamp rather than a database sequence - unique and
/// roughly time-ordered without needing a dedicated sequence-number
/// aggregate this milestone has no other use for.
/// </summary>
public sealed class OrderNumber : ValueObject
{
    private const int MinLength = 3;
    private const int MaxLength = 40;

    /// <summary>The display text (e.g. "ORD-18f3a2b7c9d1e4").</summary>
    public string Value { get; }

    private OrderNumber(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into an <see cref="OrderNumber"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or too long.</exception>
    public static OrderNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Order number is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Order number must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new OrderNumber(value);
    }

    /// <summary>Generates a new, unique order number from the given UTC instant.</summary>
    public static OrderNumber Generate(DateTimeOffset createdAtUtc) => new($"ORD-{createdAtUtc.UtcTicks:x}");

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
