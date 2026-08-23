using System;
using System.Collections.Generic;
using Clovent.Domain;

namespace Clovent.Restaurant.Tables.ValueObjects;

/// <summary>A table's display name (e.g. "Table 1", "Window Table").</summary>
public sealed class TableName : ValueObject
{
    private const int MinLength = 1;
    private const int MaxLength = 100;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private TableName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="TableName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>1</c> or longer than <c>100</c> characters.</exception>
    public static TableName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Table name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Table name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new TableName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
