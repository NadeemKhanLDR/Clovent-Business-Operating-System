namespace Clovent.Restaurant.Customers;

/// <summary>Strongly-typed identifier for a <see cref="CustomerPaymentAllocation"/>.</summary>
public readonly record struct CustomerPaymentAllocationId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("CustomerPaymentAllocationId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Generates a new, non-empty <see cref="CustomerPaymentAllocationId"/>.</summary>
    public static CustomerPaymentAllocationId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
