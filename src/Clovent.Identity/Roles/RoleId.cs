namespace Clovent.Identity.Roles;

/// <summary>Strongly-typed identifier for a <see cref="Role"/> aggregate.</summary>
public readonly record struct RoleId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("RoleId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="RoleId"/>.</summary>
    public static RoleId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
