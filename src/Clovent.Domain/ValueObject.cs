namespace Clovent.Domain;

/// <summary>
/// Base type for immutable objects identified by their attributes rather
/// than an identity - equality compares every component returned by
/// <see cref="GetEqualityComponents"/>, in order.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>The values that determine equality for this value object, in a stable order.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>Two value objects are equal when they are the same runtime type and every equality component matches, in order.</summary>
    public bool Equals(ValueObject? other) => Equals((object?)other);

    /// <inheritdoc cref="Equals(ValueObject?)"/>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc/>
    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(0, HashCode.Combine);

    /// <inheritdoc cref="Equals(ValueObject?)"/>
    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    /// <inheritdoc cref="Equals(ValueObject?)"/>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
