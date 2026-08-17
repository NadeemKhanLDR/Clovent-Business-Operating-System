using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Forms.Catalog.Products;

/// <summary>
/// Create/edit dialog for a Product - name, SKU (immutable after creation),
/// optional Category/Group/Brand classification, required base Unit of
/// Measure, and tax configuration (rate + inclusive/exclusive). Control tree
/// (fields, <c>AddField</c> calls) lives in
/// <c>ProductEditForm.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class ProductEditForm : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid?> _categoriesByDisplay;
    private readonly Dictionary<string, Guid?> _groupsByDisplay;
    private readonly Dictionary<string, Guid?> _brandsByDisplay;
    private readonly Dictionary<string, Guid?> _unitsByDisplay;

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ProductEditForm() : base("Edit Product")
    {
        _categoriesByDisplay = null!;
        _groupsByDisplay = null!;
        _brandsByDisplay = null!;
        _unitsByDisplay = null!;

        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog. <paramref name="title"/> is the dialog's caption.
    /// <paramref name="categoryOptions"/>, <paramref name="groupOptions"/>,
    /// and <paramref name="brandOptions"/> populate their respective optional
    /// classification combos; <paramref name="unitOptions"/> must be
    /// non-empty - a product cannot be created without at least one unit of
    /// measure to seed with. <paramref name="name"/>, <paramref name="sku"/>,
    /// <paramref name="categoryId"/>, <paramref name="groupId"/>,
    /// <paramref name="brandId"/>, <paramref name="baseUnitOfMeasureId"/>,
    /// <paramref name="taxRatePercentage"/>, and
    /// <paramref name="taxIsInclusive"/> pre-populate the fields when editing
    /// an existing product. The SKU field is only enabled when
    /// <paramref name="isNew"/> is <see langword="true"/>, since the SKU is
    /// immutable after creation.
    /// </summary>
    public ProductEditForm(
        string title,
        IReadOnlyList<(Guid Id, string Display)> categoryOptions,
        IReadOnlyList<(Guid Id, string Display)> groupOptions,
        IReadOnlyList<(Guid Id, string Display)> brandOptions,
        IReadOnlyList<(Guid Id, string Display)> unitOptions,
        string? name = null,
        string? sku = null,
        Guid? categoryId = null,
        Guid? groupId = null,
        Guid? brandId = null,
        Guid? baseUnitOfMeasureId = null,
        decimal taxRatePercentage = 0,
        bool taxIsInclusive = false,
        bool isNew = true) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _categoriesByDisplay = null!;
            _groupsByDisplay = null!;
            _brandsByDisplay = null!;
            _unitsByDisplay = null!;
            return;
        }

        _nameEdit.Text = name ?? string.Empty;
        _skuEdit.Text = sku ?? string.Empty;
        _skuEdit.Enabled = isNew;
        _taxRateEdit.Value = taxRatePercentage;
        _taxInclusiveEdit.Checked = taxIsInclusive;

        _categoriesByDisplay = ComboBoxBinder.Bind(_categoryCombo, categoryOptions, includeEmpty: true);
        ComboBoxBinder.SelectById(_categoryCombo, _categoriesByDisplay, categoryId);

        _groupsByDisplay = ComboBoxBinder.Bind(_groupCombo, groupOptions, includeEmpty: true);
        ComboBoxBinder.SelectById(_groupCombo, _groupsByDisplay, groupId);

        _brandsByDisplay = ComboBoxBinder.Bind(_brandCombo, brandOptions, includeEmpty: true);
        ComboBoxBinder.SelectById(_brandCombo, _brandsByDisplay, brandId);

        _unitsByDisplay = ComboBoxBinder.Bind(_unitCombo, unitOptions, includeEmpty: false);
        ComboBoxBinder.SelectById(_unitCombo, _unitsByDisplay, baseUnitOfMeasureId);
    }

    /// <summary>The entered product name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <summary>The entered SKU (only meaningful when creating).</summary>
    public string SkuValue => _skuEdit.Text.Trim();

    /// <summary>The selected category, or <see langword="null"/>.</summary>
    public Guid? CategoryId => ComboBoxBinder.GetSelectedId(_categoryCombo, _categoriesByDisplay);

    /// <summary>The selected group, or <see langword="null"/>.</summary>
    public Guid? GroupId => ComboBoxBinder.GetSelectedId(_groupCombo, _groupsByDisplay);

    /// <summary>The selected brand, or <see langword="null"/>.</summary>
    public Guid? BrandId => ComboBoxBinder.GetSelectedId(_brandCombo, _brandsByDisplay);

    /// <summary>The selected base unit of measure.</summary>
    public Guid? BaseUnitOfMeasureId => ComboBoxBinder.GetSelectedId(_unitCombo, _unitsByDisplay);

    /// <summary>The entered tax rate percentage.</summary>
    public decimal TaxRatePercentage => _taxRateEdit.Value;

    /// <summary>Whether prices are tax-inclusive.</summary>
    public bool TaxIsInclusive => _taxInclusiveEdit.Checked;

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

        if (BaseUnitOfMeasureId is null)
        {
            error = "A base unit of measure is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
