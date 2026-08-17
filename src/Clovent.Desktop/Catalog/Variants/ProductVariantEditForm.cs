using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.Variants;

/// <summary>
/// Create/edit dialog for a Product Variant - name, SKU (immutable after
/// creation), and unit of measure. Control tree (fields, <c>AddField</c>
/// calls) lives in <c>ProductVariantEditForm.Designer.cs</c>; this file
/// holds behavior only.
/// </summary>
public sealed partial class ProductVariantEditForm : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid?> _unitsByDisplay;

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ProductVariantEditForm() : base("Edit Variant")
    {
        _unitsByDisplay = null!;

        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog. <paramref name="title"/> is the dialog's caption.
    /// <paramref name="unitOptions"/> must be non-empty. <paramref name="name"/>
    /// and <paramref name="sku"/> pre-populate the fields; the SKU field is
    /// only enabled when <paramref name="isNew"/> is <see langword="true"/>,
    /// since the SKU is immutable after creation. <paramref name="unitOfMeasureId"/>
    /// selects the initial unit.
    /// </summary>
    public ProductVariantEditForm(
        string title,
        IReadOnlyList<(Guid Id, string Display)> unitOptions,
        string? name = null,
        string? sku = null,
        Guid? unitOfMeasureId = null,
        bool isNew = true) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _unitsByDisplay = null!;
            return;
        }

        _nameEdit.Text = name ?? string.Empty;
        _skuEdit.Text = sku ?? string.Empty;
        _skuEdit.Enabled = isNew;

        _unitsByDisplay = ComboBoxBinder.Bind(_unitCombo, unitOptions, includeEmpty: false);
        ComboBoxBinder.SelectById(_unitCombo, _unitsByDisplay, unitOfMeasureId);
    }

    /// <summary>The entered variant name/attribute summary.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <summary>The entered SKU (only meaningful when creating).</summary>
    public string SkuValue => _skuEdit.Text.Trim();

    /// <summary>The selected unit of measure.</summary>
    public Guid? UnitOfMeasureId => ComboBoxBinder.GetSelectedId(_unitCombo, _unitsByDisplay);

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        if (_skuEdit.Enabled && string.IsNullOrWhiteSpace(_skuEdit.Text))
        {
            error = "SKU is required.";
            return false;
        }

        if (UnitOfMeasureId is null)
        {
            error = "A unit of measure is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
