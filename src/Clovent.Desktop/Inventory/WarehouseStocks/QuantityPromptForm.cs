using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Inventory.WarehouseStocks;

/// <summary>
/// Single-field quantity prompt shared by the Receive/Issue/Reserve/Release
/// stock actions - every one of them needs nothing more than "how much".
/// Control tree (fields, <c>AddField</c> calls) lives in
/// <c>QuantityPromptForm.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class QuantityPromptForm : MasterDataEditFormBase
{
    /// <summary>Builds the dialog.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public QuantityPromptForm() : base("Enter Quantity")
    {
        InitializeComponent();
    }

    /// <summary>Builds the dialog. <paramref name="title"/> is the dialog's caption; <paramref name="label"/> is the field's label (e.g. "Quantity to Receive:").</summary>
    public QuantityPromptForm(string title, string label) : base(title)
    {
        InitializeComponent();
        label1.Text = label;

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;
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
