using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Inventory.Transfers;

/// <summary>
/// Create dialog for a Stock Transfer - source warehouse, destination
/// warehouse, variant, and quantity. There is no edit dialog: every field is
/// fixed once proposed (see <c>Clovent.Inventory.Transfers.StockTransfer</c>'s
/// doc comment) - only Complete/Cancel act on it afterward, exposed as
/// list-view actions instead. Control tree (fields, <c>AddField</c> calls)
/// lives in <c>StockTransferCreateForm.Designer.cs</c>; this file holds
/// behavior only.
/// </summary>
public sealed partial class StockTransferCreateForm : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid?> _warehousesByDisplay;
    private readonly Dictionary<string, Guid?> _variantsByDisplay;

    /// <summary>Builds the dialog.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public StockTransferCreateForm() : base("New Stock Transfer")
    {
        _warehousesByDisplay = null!;
        _variantsByDisplay = null!;

        InitializeComponent();
        }

    /// <summary>Builds the dialog. <paramref name="warehouseOptions"/> populates both the source and destination combos; <paramref name="variantOptions"/> populates the variant combo.</summary>
    public StockTransferCreateForm(IReadOnlyList<(Guid Id, string Display)> warehouseOptions, IReadOnlyList<(Guid Id, string Display)> variantOptions) : base("New Stock Transfer")
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _warehousesByDisplay = null!;
            _variantsByDisplay = null!;
            return;
        }

        _warehousesByDisplay = ComboBoxBinder.Bind(_sourceCombo, warehouseOptions, includeEmpty: false);
        ComboBoxBinder.Bind(_destinationCombo, warehouseOptions, includeEmpty: false);
        _variantsByDisplay = ComboBoxBinder.Bind(_variantCombo, variantOptions, includeEmpty: false);
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
