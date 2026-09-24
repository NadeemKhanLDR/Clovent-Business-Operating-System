using System;

namespace Clovent.Restaurant.DayClose;

/// <summary>Strongly-typed identifier for a <see cref="BusinessDayClose"/> aggregate.</summary>
public readonly record struct BusinessDayCloseId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("BusinessDayCloseId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="BusinessDayCloseId"/>.</summary>
    public static BusinessDayCloseId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
