namespace Clovent.Identity.Organizations;

/// <summary>Strongly-typed identifier for an <see cref="Organization"/> aggregate.</summary>
public readonly record struct OrganizationId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("OrganizationId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="OrganizationId"/>.</summary>
    public static OrganizationId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
