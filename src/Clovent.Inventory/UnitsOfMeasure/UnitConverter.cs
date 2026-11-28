using System;
using System.Collections.Generic;

namespace Clovent.Inventory.UnitsOfMeasure;

/// <summary>
/// Universal kitchen unit of measure conversion engine for restaurant operations.
/// Supports mass, volume, and discrete count unit conversions with 4-decimal precision (decimal(18,4)).
/// Uses MidpointRounding.AwayFromZero in accordance with the centralized financial rounding policy.
/// </summary>
public static class UnitConverter
{
    private enum UnitFamily
    {
        Mass,
        Volume,
        Count
    }

    private sealed record UnitDefinition(UnitFamily Family, decimal FactorToBase);

    // Base units: Mass -> Grams (G), Volume -> Milliliters (ML), Count -> Pieces (PCS)
    private static readonly Dictionary<string, UnitDefinition> Units = new(StringComparer.OrdinalIgnoreCase)
    {
        // Mass / Weight (Base: Gram)
        { "G", new UnitDefinition(UnitFamily.Mass, 1.0m) },
        { "GRAM", new UnitDefinition(UnitFamily.Mass, 1.0m) },
        { "GRAMS", new UnitDefinition(UnitFamily.Mass, 1.0m) },
        { "KG", new UnitDefinition(UnitFamily.Mass, 1000.0m) },
        { "KILOGRAM", new UnitDefinition(UnitFamily.Mass, 1000.0m) },
        { "KILOGRAMS", new UnitDefinition(UnitFamily.Mass, 1000.0m) },
        { "MG", new UnitDefinition(UnitFamily.Mass, 0.001m) },
        { "MILLIGRAM", new UnitDefinition(UnitFamily.Mass, 0.001m) },
        { "MILLIGRAMS", new UnitDefinition(UnitFamily.Mass, 0.001m) },
        { "LB", new UnitDefinition(UnitFamily.Mass, 453.59237m) },
        { "POUND", new UnitDefinition(UnitFamily.Mass, 453.59237m) },
        { "POUNDS", new UnitDefinition(UnitFamily.Mass, 453.59237m) },
        { "OZ", new UnitDefinition(UnitFamily.Mass, 28.349523125m) },
        { "OUNCE", new UnitDefinition(UnitFamily.Mass, 28.349523125m) },
        { "OUNCES", new UnitDefinition(UnitFamily.Mass, 28.349523125m) },

        // Volume (Base: Milliliter)
        { "ML", new UnitDefinition(UnitFamily.Volume, 1.0m) },
        { "MILLILITER", new UnitDefinition(UnitFamily.Volume, 1.0m) },
        { "MILLILITERS", new UnitDefinition(UnitFamily.Volume, 1.0m) },
        { "L", new UnitDefinition(UnitFamily.Volume, 1000.0m) },
        { "LITER", new UnitDefinition(UnitFamily.Volume, 1000.0m) },
        { "LITERS", new UnitDefinition(UnitFamily.Volume, 1000.0m) },
        { "LTR", new UnitDefinition(UnitFamily.Volume, 1000.0m) },
        { "CL", new UnitDefinition(UnitFamily.Volume, 10.0m) },
        { "CENTILITER", new UnitDefinition(UnitFamily.Volume, 10.0m) },
        { "CENTILITERS", new UnitDefinition(UnitFamily.Volume, 10.0m) },
        { "TSP", new UnitDefinition(UnitFamily.Volume, 4.92892m) },
        { "TEASPOON", new UnitDefinition(UnitFamily.Volume, 4.92892m) },
        { "TEASPOONS", new UnitDefinition(UnitFamily.Volume, 4.92892m) },
        { "TBSP", new UnitDefinition(UnitFamily.Volume, 14.7868m) },
        { "TABLESPOON", new UnitDefinition(UnitFamily.Volume, 14.7868m) },
        { "TABLESPOONS", new UnitDefinition(UnitFamily.Volume, 14.7868m) },
        { "CUP", new UnitDefinition(UnitFamily.Volume, 240.0m) },
        { "CUPS", new UnitDefinition(UnitFamily.Volume, 240.0m) },
        { "FL_OZ", new UnitDefinition(UnitFamily.Volume, 29.5735m) },
        { "FLOZ", new UnitDefinition(UnitFamily.Volume, 29.5735m) },

        // Count / Discrete Units (Base: Piece)
        { "PCS", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "PIECE", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "PIECES", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "EACH", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "ITEM", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "ITEMS", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "SLICE", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "SLICES", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "PORTION", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "PORTIONS", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "SERVING", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "SERVINGS", new UnitDefinition(UnitFamily.Count, 1.0m) },
        { "DOZEN", new UnitDefinition(UnitFamily.Count, 12.0m) },
        { "DZ", new UnitDefinition(UnitFamily.Count, 12.0m) }
    };

    /// <summary>
    /// Checks whether the source unit can be converted to the target unit.
    /// </summary>
    public static bool CanConvert(string? fromUnit, string? toUnit)
    {
        if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit))
            return false;

        fromUnit = Normalize(fromUnit);
        toUnit = Normalize(toUnit);

        if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!Units.TryGetValue(fromUnit, out var fromDef) || !Units.TryGetValue(toUnit, out var toDef))
            return false;

        return fromDef.Family == toDef.Family;
    }

    /// <summary>
    /// Converts a quantity from one unit of measure to another.
    /// </summary>
    /// <param name="quantity">Quantity to convert.</param>
    /// <param name="fromUnit">Source unit code.</param>
    /// <param name="toUnit">Target unit code.</param>
    /// <returns>Converted quantity rounded to 4 decimal places.</returns>
    /// <exception cref="ArgumentException">Units are incompatible or unknown.</exception>
    public static decimal Convert(decimal quantity, string fromUnit, string toUnit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromUnit);
        ArgumentException.ThrowIfNullOrWhiteSpace(toUnit);

        fromUnit = Normalize(fromUnit);
        toUnit = Normalize(toUnit);

        if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
            return Math.Round(quantity, 4, MidpointRounding.AwayFromZero);

        if (!Units.TryGetValue(fromUnit, out var fromDef))
            throw new ArgumentException($"Unknown source unit of measure: '{fromUnit}'.", nameof(fromUnit));

        if (!Units.TryGetValue(toUnit, out var toDef))
            throw new ArgumentException($"Unknown destination unit of measure: '{toUnit}'.", nameof(toUnit));

        if (fromDef.Family != toDef.Family)
            throw new InvalidOperationException($"Cannot convert between different unit families: '{fromUnit}' ({fromDef.Family}) and '{toUnit}' ({toDef.Family}).");

        // Convert from source to base, then base to target
        decimal baseQuantity = quantity * fromDef.FactorToBase;
        decimal targetQuantity = baseQuantity / toDef.FactorToBase;

        return Math.Round(targetQuantity, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Attempts to convert a quantity from one unit of measure to another.
    /// Returns false if units are incompatible or unknown.
    /// </summary>
    public static bool TryConvert(decimal quantity, string fromUnit, string toUnit, out decimal converted)
    {
        try
        {
            converted = Convert(quantity, fromUnit, toUnit);
            return true;
        }
        catch
        {
            converted = quantity;
            return false;
        }
    }

    /// <summary>
    /// Standardizes a unit string by trimming and upper-casing common abbreviations.
    /// </summary>
    public static string Normalize(string unit)
    {
        var trimmed = unit.Trim();
        return trimmed.ToUpperInvariant();
    }
}
