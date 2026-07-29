using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Inventory.WarehouseStocks;

/// <summary>
/// Single-field quantity prompt shared by the Receive/Issue/Reserve/Release
/// stock actions - every one of them needs nothing more than "how much".
/// </summary>
public sealed class QuantityPromptForm : MasterDataEditFormBase
{
    private readonly SpinEdit _quantityEdit = new() { Properties = { MinValue = 0.0001m, MaxValue = 1_000_000 } };

    /// <summary>Builds the dialog.</summary>
    public QuantityPromptForm(string title, string label) : base(title)
    {
        AddField(label, _quantityEdit);
    }

    /// <summary>The entered quantity.</summary>
    public decimal Quantity => _quantityEdit.Value;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (Quantity <= 0)
        {
            error = "Quantity must be positive.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
