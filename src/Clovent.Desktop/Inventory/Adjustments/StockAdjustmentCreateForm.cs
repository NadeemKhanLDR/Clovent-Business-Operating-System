using Clovent.Desktop.MasterData;
using Clovent.Inventory.Adjustments;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Inventory.Adjustments;

/// <summary>
/// Create dialog for a Stock Adjustment - variant, direction
/// (Increase/Decrease), quantity, and reason. There is no edit dialog: every
/// field is fixed once proposed (see <c>Clovent.Inventory.Adjustments.StockAdjustment</c>'s
/// doc comment) - only <see cref="Clovent.Inventory.Application.Adjustments.Commands.ApplyStockAdjustmentCommand"/>
/// or <see cref="Clovent.Inventory.Application.Adjustments.Commands.CancelStockAdjustmentCommand"/>
/// act on it afterward, both exposed as list-view actions instead.
/// </summary>
public sealed class StockAdjustmentCreateForm : MasterDataEditFormBase
{
    private readonly ComboBoxEdit _variantCombo = new();
    private readonly Dictionary<string, Guid?> _variantsByDisplay;
    private readonly ComboBoxEdit _typeCombo = new();
    private readonly SpinEdit _quantityEdit = new() { Properties = { MinValue = 0.0001m, MaxValue = 1_000_000 } };
    private readonly MemoEdit _reasonEdit = new() { Height = 60 };

    /// <summary>Builds the dialog.</summary>
    public StockAdjustmentCreateForm(IReadOnlyList<(Guid Id, string Display)> variantOptions) : base("New Stock Adjustment")
    {
        _variantsByDisplay = ComboBoxBinder.Bind(_variantCombo, variantOptions, includeEmpty: false);

        _typeCombo.Properties.Items.AddRange([nameof(StockAdjustmentType.Increase), nameof(StockAdjustmentType.Decrease)]);
        _typeCombo.SelectedIndex = 0;

        AddField("Variant:", _variantCombo);
        AddField("Type:", _typeCombo);
        AddField("Quantity:", _quantityEdit);
        AddField("Reason:", _reasonEdit);
    }

    /// <summary>The selected variant.</summary>
    public Guid? VariantId => ComboBoxBinder.GetSelectedId(_variantCombo, _variantsByDisplay);

    /// <summary>The selected adjustment direction.</summary>
    public StockAdjustmentType AdjustmentType => Enum.Parse<StockAdjustmentType>((string)_typeCombo.SelectedItem);

    /// <summary>The entered quantity.</summary>
    public decimal Quantity => _quantityEdit.Value;

    /// <summary>The entered reason.</summary>
    public string Reason => _reasonEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (VariantId is null)
        {
            error = "Select a variant.";
            return false;
        }

        if (Quantity <= 0)
        {
            error = "Quantity must be positive.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_reasonEdit.Text))
        {
            error = "Reason is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
