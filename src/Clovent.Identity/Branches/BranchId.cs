namespace Clovent.Identity.Branches;

/// <summary>Strongly-typed identifier for a <see cref="Branch"/> aggregate.</summary>
public readonly record struct BranchId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("BranchId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="BranchId"/>.</summary>
    public static BranchId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
