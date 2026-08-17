namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// The one currently-configured <c>Currency</c>'s symbol/decimal places
/// (<c>MasterData.Application.Currencies</c>, unchanged), held statically so
/// every Restaurant screen's grid/label formats money the same way instead
/// of each re-picking its own "N2"/"$"/etc. Screens already resolve "the
/// first configured currency" for their own writes (<c>MenuItemsForm</c>,
/// <c>RestaurantPosView</c> - see <c>RestaurantPOSArchitecture.md</c>
/// Section 13.1) - <see cref="Configure"/> is called once per screen load
/// with that same currency, so display and write path never disagree. A
/// static holder (not per-instance state) is deliberate: this is a
/// single-user desktop process with exactly one configured currency at a
/// time, the same "fine at this scale" reasoning already applied throughout
/// this codebase's Restaurant screens.
/// </summary>
public static class CurrencyDisplay
{
    private static string _symbol = string.Empty;
    private static int _decimalPlaces = 2;

    /// <summary>Sets the symbol/decimal places every subsequent <see cref="Format"/> call uses.</summary>
    public static void Configure(string symbol, int decimalPlaces)
    {
        _symbol = symbol;
        _decimalPlaces = decimalPlaces;
    }

    /// <summary>Formats <paramref name="amount"/> as currency ("Rs.850.00") using the last-configured symbol/decimal places, or plain "N2" if none has been configured yet.</summary>
    public static string Format(decimal amount)
    {
        var numeric = amount.ToString("N" + Math.Clamp(_decimalPlaces, 0, 4));
        return string.IsNullOrEmpty(_symbol) ? numeric : $"{_symbol}{numeric}";
    }
}
