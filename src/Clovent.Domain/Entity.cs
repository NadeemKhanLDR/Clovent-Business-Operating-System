namespace Clovent.Domain;

/// <summary>
/// Base type for anything with a distinct, stable identity rather than
/// value-based equality. Equality compares identity and runtime type only -
/// never the entity's mutable state.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for this entity.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>The entity's identity, assigned once at creation.</summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>Two entities are equal when they are the same runtime type and share the same <see cref="Id"/>.</summary>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc cref="Equals(Entity{TId}?)"/>
    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <inheritdoc cref="Equals(Entity{TId}?)"/>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <inheritdoc cref="Equals(Entity{TId}?)"/>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
