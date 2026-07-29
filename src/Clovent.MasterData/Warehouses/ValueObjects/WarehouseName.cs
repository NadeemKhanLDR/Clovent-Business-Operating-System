using Clovent.Domain;

namespace Clovent.MasterData.Warehouses.ValueObjects;

/// <summary>A warehouse's display name.</summary>
public sealed class WarehouseName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private WarehouseName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="WarehouseName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>100</c> characters.</exception>
    public static WarehouseName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Warehouse name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Warehouse name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new WarehouseName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
