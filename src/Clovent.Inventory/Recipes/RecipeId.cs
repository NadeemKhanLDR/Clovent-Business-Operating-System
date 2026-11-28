using System;

namespace Clovent.Inventory.Recipes;

/// <summary>Strongly-typed identifier for a <see cref="Recipe"/> aggregate.</summary>
public readonly record struct RecipeId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("RecipeId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="RecipeId"/>.</summary>
    public static RecipeId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
