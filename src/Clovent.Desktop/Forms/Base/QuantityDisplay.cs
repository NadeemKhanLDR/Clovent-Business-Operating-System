using System;
using System.Globalization;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Centralized display helper for stock and inventory quantities.
/// Formats numeric quantities using the company-configured display precision (default 2 decimals, e.g. "145.00").
/// Never applies currency symbols or currency prefixes.
/// </summary>
public static class QuantityDisplay
{
    private static int _precision = 2;

    /// <summary>Gets the configured decimal precision for quantity display.</summary>
    public static int Precision => _precision;

    /// <summary>Sets the display precision for all subsequent quantity formatting.</summary>
    public static void Configure(int precision)
    {
        _precision = Math.Clamp(precision, 0, 4);
    }

    /// <summary>Formats a nullable quantity.</summary>
    public static string Format(decimal? quantity)
    {
        if (quantity == null) return "-";
        return Format(quantity.Value);
    }

    /// <summary>Formats a quantity with thousands separators according to the configured precision (e.g. "145.00").</summary>
    public static string Format(decimal quantity)
    {
        return quantity.ToString("N" + _precision, CultureInfo.InvariantCulture);
    }

    /// <summary>Formats a quantity without thousands separators according to the configured precision.</summary>
    public static string FormatPlain(decimal quantity)
    {
        return quantity.ToString("F" + _precision, CultureInfo.InvariantCulture);
    }
}
