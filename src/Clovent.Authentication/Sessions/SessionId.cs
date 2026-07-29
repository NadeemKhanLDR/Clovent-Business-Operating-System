namespace Clovent.Authentication.Sessions;

/// <summary>Strongly-typed identifier for a <see cref="Session"/> aggregate.</summary>
public readonly record struct SessionId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("SessionId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="SessionId"/>.</summary>
    public static SessionId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
