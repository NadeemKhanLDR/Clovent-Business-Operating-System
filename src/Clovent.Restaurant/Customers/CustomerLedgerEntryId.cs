namespace Clovent.Restaurant.Customers;

/// <summary>Strongly-typed identifier for a <see cref="CustomerLedgerEntry"/>.</summary>
public readonly record struct CustomerLedgerEntryId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("CustomerLedgerEntryId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="CustomerLedgerEntryId"/>.</summary>
    public static CustomerLedgerEntryId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
