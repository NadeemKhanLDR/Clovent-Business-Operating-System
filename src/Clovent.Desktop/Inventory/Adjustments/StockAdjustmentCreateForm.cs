using Clovent.Desktop.MasterData;
using Clovent.Inventory.Adjustments;

namespace Clovent.Desktop.Inventory.Adjustments;

/// <summary>
/// Create dialog for a Stock Adjustment - variant, direction
/// (Increase/Decrease), quantity, and reason. There is no edit dialog: every
/// field is fixed once proposed (see <c>Clovent.Inventory.Adjustments.StockAdjustment</c>'s
/// doc comment) - only <see cref="Clovent.Inventory.Application.Adjustments.Commands.ApplyStockAdjustmentCommand"/>
/// or <see cref="Clovent.Inventory.Application.Adjustments.Commands.CancelStockAdjustmentCommand"/>
/// act on it afterward, both exposed as list-view actions instead. Control
/// tree (fields, <c>AddField</c> calls) lives in
/// <c>StockAdjustmentCreateForm.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class StockAdjustmentCreateForm : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid?> _variantsByDisplay;

    /// <summary>Builds the dialog.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public StockAdjustmentCreateForm() : base("New Stock Adjustment")
    {
        _variantsByDisplay = null!;

        InitializeComponent();
        }

    /// <summary>Builds the dialog. <paramref name="variantOptions"/> populates the variant combo.</summary>
    public StockAdjustmentCreateForm(IReadOnlyList<(Guid Id, string Display)> variantOptions) : base("New Stock Adjustment")
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _variantsByDisplay = null!;
            return;
        }

        _variantsByDisplay = ComboBoxBinder.Bind(_variantCombo, variantOptions, includeEmpty: false);
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
