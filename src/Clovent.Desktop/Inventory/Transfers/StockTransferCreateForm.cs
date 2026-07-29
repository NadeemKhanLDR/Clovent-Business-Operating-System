using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Inventory.Transfers;

/// <summary>
/// Create dialog for a Stock Transfer - source warehouse, destination
/// warehouse, variant, and quantity. There is no edit dialog: every field is
/// fixed once proposed (see <c>Clovent.Inventory.Transfers.StockTransfer</c>'s
/// doc comment) - only Complete/Cancel act on it afterward, exposed as
/// list-view actions instead.
/// </summary>
public sealed class StockTransferCreateForm : MasterDataEditFormBase
{
    private readonly ComboBoxEdit _sourceCombo = new();
    private readonly ComboBoxEdit _destinationCombo = new();
    private readonly ComboBoxEdit _variantCombo = new();
    private readonly Dictionary<string, Guid?> _warehousesByDisplay;
    private readonly Dictionary<string, Guid?> _variantsByDisplay;
    private readonly SpinEdit _quantityEdit = new() { Properties = { MinValue = 0.0001m, MaxValue = 1_000_000 } };

    /// <summary>Builds the dialog.</summary>
    public StockTransferCreateForm(IReadOnlyList<(Guid Id, string Display)> warehouseOptions, IReadOnlyList<(Guid Id, string Display)> variantOptions)
        : base("New Stock Transfer")
    {
        _warehousesByDisplay = ComboBoxBinder.Bind(_sourceCombo, warehouseOptions, includeEmpty: false);
        ComboBoxBinder.Bind(_destinationCombo, warehouseOptions, includeEmpty: false);
        _variantsByDisplay = ComboBoxBinder.Bind(_variantCombo, variantOptions, includeEmpty: false);

        AddField("Source Warehouse:", _sourceCombo);
        AddField("Destination Warehouse:", _destinationCombo);
        AddField("Variant:", _variantCombo);
        AddField("Quantity:", _quantityEdit);
    }

    /// <summary>The selected source warehouse.</summary>
    public Guid? SourceWarehouseId => ComboBoxBinder.GetSelectedId(_sourceCombo, _warehousesByDisplay);

    /// <summary>The selected destination warehouse.</summary>
    public Guid? DestinationWarehouseId => ComboBoxBinder.GetSelectedId(_destinationCombo, _warehousesByDisplay);

    /// <summary>The selected variant.</summary>
    public Guid? VariantId => ComboBoxBinder.GetSelectedId(_variantCombo, _variantsByDisplay);

    /// <summary>The entered quantity.</summary>
    public decimal Quantity => _quantityEdit.Value;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (SourceWarehouseId is null || DestinationWarehouseId is null)
        {
            error = "Select both a source and destination warehouse.";
            return false;
        }

        if (SourceWarehouseId == DestinationWarehouseId)
        {
            error = "Source and destination warehouses must differ.";
            return false;
        }

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

        error = string.Empty;
        return true;
    }
}
