namespace Clovent.Identity.Companies;

/// <summary>Strongly-typed identifier for a <see cref="Company"/> aggregate.</summary>
public readonly record struct CompanyId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("CompanyId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="CompanyId"/>.</summary>
    public static CompanyId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
