using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Inventory.WarehouseStocks;

/// <summary>
/// "Receive Inventory" / Opening Stock dialog: Warehouse + Product + Quantity
/// + Notes, sent as one <c>OpenOrReceiveStockCommand</c> - the atomic
/// alternative to the row-only "Receive" action, which requires a
/// <see cref="Clovent.Inventory.WarehouseStocks.WarehouseStock"/> row to
/// already exist. Warehouse defaults to whichever one the screen's picker
/// currently has selected, but can be changed here.
/// </summary>
public sealed class ReceiveInventoryForm : MasterDataEditFormBase
{
    private readonly ComboBoxEdit _warehouseCombo = new();
    private readonly Dictionary<string, Guid?> _warehousesByDisplay;
    private readonly ComboBoxEdit _variantCombo = new();
    private readonly Dictionary<string, Guid?> _variantsByDisplay;
    private readonly SpinEdit _quantityEdit = new() { Properties = { MinValue = 0.0001m, MaxValue = 1_000_000 } };
    private readonly TextEdit _notesEdit = new();

    /// <summary>Builds the dialog.</summary>
    public ReceiveInventoryForm(
        IReadOnlyList<(Guid Id, string Display)> warehouseOptions,
        Guid? selectedWarehouseId,
        IReadOnlyList<(Guid Id, string Display)> variantOptions) : base("Receive Inventory")
    {
        Height = 300;

        _warehousesByDisplay = ComboBoxBinder.Bind(_warehouseCombo, warehouseOptions, includeEmpty: false);
        ComboBoxBinder.SelectById(_warehouseCombo, _warehousesByDisplay, selectedWarehouseId);
        _variantsByDisplay = ComboBoxBinder.Bind(_variantCombo, variantOptions, includeEmpty: false);

        AddField("Warehouse:", _warehouseCombo);
        AddField("Product:", _variantCombo);
        AddField("Quantity:", _quantityEdit);
        AddField("Notes:", _notesEdit);
    }

    /// <summary>The selected warehouse id.</summary>
    public Guid? WarehouseId => ComboBoxBinder.GetSelectedId(_warehouseCombo, _warehousesByDisplay);

    /// <summary>The selected product variant id.</summary>
    public Guid? ProductVariantId => ComboBoxBinder.GetSelectedId(_variantCombo, _variantsByDisplay);

    /// <summary>The entered quantity.</summary>
    public decimal Quantity => _quantityEdit.Value;

    /// <summary>The entered notes, or <see langword="null"/> if left blank.</summary>
    public string? Notes => string.IsNullOrWhiteSpace(_notesEdit.Text) ? null : _notesEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (WarehouseId is null)
        {
            error = "Select a warehouse.";
            return false;
        }

        if (ProductVariantId is null)
        {
            error = "Select a product.";
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
