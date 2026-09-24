namespace Clovent.Restaurant.QuickOrderTemplates;

/// <summary>Strongly-typed identifier for a <see cref="QuickOrderTemplateItem"/> entity.</summary>
public readonly record struct QuickOrderTemplateItemId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("QuickOrderTemplateItemId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="QuickOrderTemplateItemId"/>.</summary>
    public static QuickOrderTemplateItemId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
