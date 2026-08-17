using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Prompt for overriding a single order line's price - "Half Chicken Karahi,
/// make it 300 this time" - shows the item's current (catalog) price for
/// reference and asks for the new price plus a required reason, which
/// <see cref="Clovent.Restaurant.Application.OrderLines.Commands.OverrideOrderLinePriceCommand"/>
/// records permanently alongside the original price, the cashier, and the
/// time (see <c>OrderLine.OverridePrice</c>'s doc comment) for audit
/// purposes. Control tree lives in <c>PriceOverrideDialog.Designer.cs</c>;
/// this file holds behavior only.
/// </summary>
public sealed partial class PriceOverrideDialog : MasterDataEditFormBase
{
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public PriceOverrideDialog() : base("Override Price")
    {
        InitializeComponent();
        }

    /// <summary>Builds the dialog for <paramref name="itemName"/>, a line currently priced at <paramref name="currentPrice"/>.</summary>
    public PriceOverrideDialog(string itemName, decimal currentPrice) : base($"Override Price - {itemName}")
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _currentPriceLabel.Text = $"Current price: {CurrencyDisplay.Format(currentPrice)}";
        _newPriceEdit.Value = currentPrice;
    }

    /// <summary>The entered new unit price.</summary>
    public decimal NewPrice => _newPriceEdit.Value;

    /// <summary>The entered reason - required, kept for the audit trail.</summary>
    public string Reason => _reasonEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (NewPrice < 0)
        {
            error = "Enter a price of zero or more.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_reasonEdit.Text))
        {
            error = "Enter a reason for this price override (kept for audit purposes).";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
