namespace Clovent.Identity.Users;

/// <summary>Strongly-typed identifier for a <see cref="User"/> aggregate.</summary>
public readonly record struct UserId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("UserId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="UserId"/>.</summary>
    public static UserId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
