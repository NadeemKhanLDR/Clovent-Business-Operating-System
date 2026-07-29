namespace Clovent.Identity.Permissions;

/// <summary>Strongly-typed identifier for a <see cref="Permission"/> aggregate.</summary>
public readonly record struct PermissionId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("PermissionId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="PermissionId"/>.</summary>
    public static PermissionId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
