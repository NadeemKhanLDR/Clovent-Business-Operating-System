namespace Clovent.Authentication.RefreshSessions;

/// <summary>Strongly-typed identifier for a <see cref="RefreshSession"/> aggregate.</summary>
public readonly record struct RefreshSessionId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("RefreshSessionId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="RefreshSessionId"/>.</summary>
    public static RefreshSessionId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
