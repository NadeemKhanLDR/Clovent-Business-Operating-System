using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Catalog.Variants;

/// <summary>Create/edit dialog for a Product Variant - name, SKU (immutable after creation), and unit of measure.</summary>
public sealed class ProductVariantEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _nameEdit = new();
    private readonly TextEdit _skuEdit = new();
    private readonly ComboBoxEdit _unitCombo = new();
    private readonly Dictionary<string, Guid?> _unitsByDisplay;

    /// <summary>Builds the dialog. <paramref name="unitOptions"/> must be non-empty.</summary>
    public ProductVariantEditForm(
        string title,
        IReadOnlyList<(Guid Id, string Display)> unitOptions,
        string? name = null,
        string? sku = null,
        Guid? unitOfMeasureId = null,
        bool isNew = true) : base(title)
    {
        _nameEdit.Text = name ?? string.Empty;
        _skuEdit.Text = sku ?? string.Empty;
        _skuEdit.Enabled = isNew;

        _unitsByDisplay = ComboBoxBinder.Bind(_unitCombo, unitOptions, includeEmpty: false);
        ComboBoxBinder.SelectById(_unitCombo, _unitsByDisplay, unitOfMeasureId);

        AddField("Name:", _nameEdit);
        AddField("SKU:", _skuEdit);
        AddField("Unit:", _unitCombo);
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
