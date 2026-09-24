using Clovent.Desktop.Forms.Base;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// One selectable row in the Suggested Add-ons grid. Presentation model
/// only - the VariantId travels with the row so the POS form can push the
/// selection through its normal add pipeline, but no id is ever displayed.
/// </summary>
public sealed record SuggestedAddOnRow(
    Guid VariantId,
    string ItemName,
    string PortionName,
    decimal UnitPrice);

/// <summary>
/// Resolves the POS display naming for a recommendation (product name plus
/// cleaned portion), shared by the embedded suggestion grid and the cart
/// so both show identical names.
/// </summary>
public static class SuggestedAddOnNaming
{
    /// <summary>
    /// Splits a product/variant pair into the full item name and the
    /// portion caption ("Half", "Full", or "-" when the variant adds no
    /// distinguishing portion), mirroring the cart's naming conventions.
    /// </summary>
    public static (string ItemName, string PortionName) Resolve(string productName, string variantName)
    {
        if (string.IsNullOrWhiteSpace(variantName)
            || variantName.Equals(productName, StringComparison.OrdinalIgnoreCase))
        {
            return (productName, "-");
        }

        var portion = variantName;
        if (portion.StartsWith(productName, StringComparison.OrdinalIgnoreCase))
        {
            portion = portion[productName.Length..].TrimStart(' ', '-', ':');
        }

        if (portion.Equals("Half Plate", StringComparison.OrdinalIgnoreCase))
        {
            portion = "Half";
        }
        else if (portion.Equals("Full Plate", StringComparison.OrdinalIgnoreCase))
        {
            portion = "Full";
        }

        if (string.IsNullOrEmpty(portion)
            || portion.Equals("Regular", StringComparison.OrdinalIgnoreCase)
            || portion.Equals("Standard", StringComparison.OrdinalIgnoreCase))
        {
            return (productName, "-");
        }

        return (productName, portion);
    }
}

/// <summary>
/// Shared UI helpers for the embedded Smart Upsell suggestion grid.
/// </summary>
public static class SuggestedAddOnUiHelper
{
    /// <summary>Composes the footer button caption with the selected count.</summary>
    public static string ComposeAddSelectedText(int count) => $"Add Selected ({count})";
}

/// <summary>Mutable grid row — checkbox state plus the display model.</summary>
public sealed class SuggestedAddOnSelectionRow(SuggestedAddOnRow row)
{
    public SuggestedAddOnRow Row { get; } = row;
    public bool Selected { get; set; }
    public string ItemName => Row.ItemName;
    public string PortionName => Row.PortionName;
    public string PriceText => CurrencyDisplay.FormatPlain(Row.UnitPrice);
}
