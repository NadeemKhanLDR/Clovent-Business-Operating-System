namespace Clovent.Restaurant.QuickOrderTemplates;

/// <summary>Strongly-typed identifier for a <see cref="QuickOrderTemplate"/> aggregate.</summary>
public readonly record struct QuickOrderTemplateId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("QuickOrderTemplateId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="QuickOrderTemplateId"/>.</summary>
    public static QuickOrderTemplateId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
