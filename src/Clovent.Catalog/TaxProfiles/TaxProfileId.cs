namespace Clovent.Catalog.TaxProfiles;

/// <summary>Strongly-typed identifier for a <see cref="TaxProfile"/> aggregate.</summary>
public readonly record struct TaxProfileId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("TaxProfileId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="TaxProfileId"/>.</summary>
    public static TaxProfileId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
