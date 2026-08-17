using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Inventory.WarehouseStocks;

/// <summary>
/// "Receive Inventory" / Opening Stock dialog: Warehouse + Product + Quantity
/// + Notes, sent as one <c>OpenOrReceiveStockCommand</c> - the atomic
/// alternative to the row-only "Receive" action, which requires a
/// <see cref="Clovent.Inventory.WarehouseStocks.WarehouseStock"/> row to
/// already exist. Warehouse defaults to whichever one the screen's picker
/// currently has selected, but can be changed here. Control tree (fields,
/// <c>AddField</c> calls) lives in <c>ReceiveInventoryForm.Designer.cs</c>;
/// this file holds behavior only.
/// </summary>
public sealed partial class ReceiveInventoryForm : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid?> _warehousesByDisplay;
    private readonly Dictionary<string, Guid?> _variantsByDisplay;

    /// <summary>Builds the dialog.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ReceiveInventoryForm() : base("Receive Inventory")
    {
        _warehousesByDisplay = null!;
        _variantsByDisplay = null!;

        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog. <paramref name="warehouseOptions"/> populates the
    /// warehouse combo, pre-selected to <paramref name="selectedWarehouseId"/>
    /// (typically the screen's currently-selected warehouse, but changeable
    /// here). <paramref name="variantOptions"/> populates the product combo.
    /// </summary>
    public ReceiveInventoryForm(
        IReadOnlyList<(Guid Id, string Display)> warehouseOptions,
        Guid? selectedWarehouseId,
        IReadOnlyList<(Guid Id, string Display)> variantOptions) : base("Receive Inventory")
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _warehousesByDisplay = null!;
            _variantsByDisplay = null!;
            return;
        }

        _warehousesByDisplay = ComboBoxBinder.Bind(_warehouseCombo, warehouseOptions, includeEmpty: false);
        ComboBoxBinder.SelectById(_warehouseCombo, _warehousesByDisplay, selectedWarehouseId);
        _variantsByDisplay = ComboBoxBinder.Bind(_variantCombo, variantOptions, includeEmpty: false);
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
