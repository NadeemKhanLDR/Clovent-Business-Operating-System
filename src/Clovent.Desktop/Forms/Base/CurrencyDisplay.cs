using System;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// The one currently-configured <c>Currency</c>'s symbol/decimal places
/// (<c>MasterData.Application.Currencies</c>), held statically so
/// every Restaurant screen's grid/label formats money consistently.
/// </summary>
public static class CurrencyDisplay
{
    private static string _code = "PKR";
    private static string _symbol = "Rs.";
    private static int _decimalPlaces = 2;

    /// <summary>Sets the symbol/decimal places every subsequent <see cref="Format"/> call uses.</summary>
    public static void Configure(string symbol, int decimalPlaces)
    {
        _symbol = symbol ?? string.Empty;
        _decimalPlaces = decimalPlaces;
    }

    /// <summary>Sets the currency code, symbol, and decimal places.</summary>
    public static void Configure(string code, string symbol, int decimalPlaces)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            _code = code.Trim();
        }
        _symbol = symbol ?? string.Empty;
        _decimalPlaces = decimalPlaces;
    }

    /// <summary>The configured ISO 4217 currency code (e.g. "PKR", "USD").</summary>
    public static string CurrencyCode => _code;

    /// <summary>The configured currency symbol (e.g. "Rs.", "$").</summary>
    public static string Symbol => _symbol;

    /// <summary>The preferred short display label: symbol if present, otherwise code.</summary>
    public static string SymbolOrCode => !string.IsNullOrWhiteSpace(_symbol) ? _symbol : _code;

    /// <summary>Formats <paramref name="amount"/> as currency (e.g. "Rs. 850.00" or "$850.00") using the configured symbol and decimal places.</summary>
    public static string Format(decimal amount)
    {
        var numeric = amount.ToString("N" + Math.Clamp(_decimalPlaces, 0, 4));
        var sym = SymbolOrCode;
        if (string.IsNullOrWhiteSpace(sym)) return numeric;

        bool needsSpace = !sym.EndsWith(" ") && !sym.EndsWith(".") && !sym.EndsWith("$") && !sym.EndsWith("€") && !sym.EndsWith("£") && !sym.EndsWith("¥");
        return needsSpace ? $"{sym} {numeric}" : $"{sym}{numeric}";
    }

    /// <summary>
    /// Formats <paramref name="amount"/> as a bare number ("850.00") using the
    /// configured decimal places without symbol.
    /// </summary>
    public static string FormatPlain(decimal amount) => amount.ToString("N" + DecimalPlaces);

    /// <summary>The currently configured number of decimal places.</summary>
    public static int DecimalPlaces => Math.Clamp(_decimalPlaces, 0, 4);
}
