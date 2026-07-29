namespace Clovent.Authentication.Credentials;

/// <summary>Strongly-typed identifier for a <see cref="UserCredentials"/> aggregate.</summary>
public readonly record struct UserCredentialsId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("UserCredentialsId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="UserCredentialsId"/>.</summary>
    public static UserCredentialsId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
