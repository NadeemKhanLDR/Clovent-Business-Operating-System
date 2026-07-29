using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Inventory.WarehouseStocks;

/// <summary>
/// Create/edit dialog for a Warehouse Stock record - minimum/maximum stock
/// levels and the negative-stock policy, plus (only when creating) which
/// variant the record tracks - fixed at creation, so no variant field shows
/// when editing an existing record. Quantity on hand/reserved are changed
/// only through the Receive/Issue/Reserve/Release actions, never edited
/// directly here.
/// </summary>
public sealed class WarehouseStockEditForm : MasterDataEditFormBase
{
    private readonly ComboBoxEdit? _variantCombo;
    private readonly Dictionary<string, Guid?>? _variantsByDisplay;
    private readonly SpinEdit _minimumStockEdit = new() { Properties = { MinValue = 0, MaxValue = 1_000_000 } };
    private readonly SpinEdit _maximumStockEdit = new() { Properties = { MinValue = 0, MaxValue = 1_000_000 } };
    private readonly CheckEdit _allowNegativeStockEdit = new() { Text = "Allow negative stock" };

    /// <summary>Builds the create dialog, with a variant picker.</summary>
    public WarehouseStockEditForm(string title, IReadOnlyList<(Guid Id, string Display)> variantOptions) : this(title, 0, 0, false)
    {
        _variantCombo = new ComboBoxEdit();
        _variantsByDisplay = ComboBoxBinder.Bind(_variantCombo, variantOptions, includeEmpty: false);
        AddField("Variant:", _variantCombo);
    }

    /// <summary>Builds the edit dialog - levels and policy only.</summary>
    public WarehouseStockEditForm(string title, decimal minimumStock, decimal maximumStock, bool allowNegativeStock) : base(title)
    {
        _minimumStockEdit.Value = minimumStock;
        _maximumStockEdit.Value = maximumStock;
        _allowNegativeStockEdit.Checked = allowNegativeStock;

        AddField("Minimum Stock:", _minimumStockEdit);
        AddField("Maximum Stock:", _maximumStockEdit);
        AddField(string.Empty, _allowNegativeStockEdit);
    }

    /// <summary>The selected variant (only meaningful when creating).</summary>
    public Guid? VariantId => _variantCombo is not null && _variantsByDisplay is not null
        ? ComboBoxBinder.GetSelectedId(_variantCombo, _variantsByDisplay)
        : null;

    /// <summary>The entered minimum stock level.</summary>
    public decimal MinimumStock => _minimumStockEdit.Value;

    /// <summary>The entered maximum stock level.</summary>
    public decimal MaximumStock => _maximumStockEdit.Value;

    /// <summary>Whether stock may go negative.</summary>
    public bool AllowNegativeStock => _allowNegativeStockEdit.Checked;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (_variantCombo is not null && VariantId is null)
        {
            error = "Select a variant.";
            return false;
        }

        if (MaximumStock > 0 && MaximumStock < MinimumStock)
        {
            error = "Maximum stock cannot be less than minimum stock.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
