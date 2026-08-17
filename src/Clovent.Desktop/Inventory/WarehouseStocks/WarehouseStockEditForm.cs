using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Inventory.WarehouseStocks;

/// <summary>
/// Create/edit dialog for a Warehouse Stock record - minimum/maximum stock
/// levels and the negative-stock policy, plus (only when creating) which
/// variant the record tracks - fixed at creation, so no variant field shows
/// when editing an existing record. Quantity on hand/reserved are changed
/// only through the Receive/Issue/Reserve/Release actions, never edited
/// directly here. Control tree (fields, <c>AddField</c> calls) lives in
/// <c>WarehouseStockEditForm.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class WarehouseStockEditForm : MasterDataEditFormBase
{
    
    private readonly Dictionary<string, Guid?>? _variantsByDisplay;

    /// <summary>Builds the create dialog, with a variant picker.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public WarehouseStockEditForm() : base("Edit Warehouse Stock")
    {
        InitializeComponent();
        }

    /// <summary>Builds the create dialog. <paramref name="title"/> is the dialog's caption; <paramref name="variantOptions"/> populates the variant picker, since which variant the record tracks is fixed at creation.</summary>
    public WarehouseStockEditForm(string title, IReadOnlyList<(Guid Id, string Display)> variantOptions) : this(title, 0, 0, false)
    {
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        
        _variantsByDisplay = ComboBoxBinder.Bind(_variantCombo, variantOptions, includeEmpty: false);
        _variantCombo.Visible = true;
        _variantLabel.Visible = true;
    }

    /// <summary>Builds the edit dialog - levels and policy only.</summary>
    public WarehouseStockEditForm(string title, decimal minimumStock, decimal maximumStock, bool allowNegativeStock) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _minimumStockEdit.Value = minimumStock;
        _maximumStockEdit.Value = maximumStock;
        _allowNegativeStockEdit.Checked = allowNegativeStock;
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
