using System.ComponentModel;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Forms.Restaurant.MenuItems;

public sealed class MenuItemVariantEditRow
{
    public Guid ProductVariantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Barcode1 { get; set; }
    public string? Barcode2 { get; set; }
    public string? Barcode3 { get; set; }
    public bool IsNew { get; set; }
    public bool IsActive { get; set; } = true;

    // Helper properties for UI binding
    public string PortionName
    {
        get
        {
            int idx = Name.IndexOf('|');
            return idx >= 0 ? Name.Substring(0, idx) : Name;
        }
        set
        {
            Name = value + "|" + PosLabel;
        }
    }

    public string PosLabel
    {
        get
        {
            int idx = Name.IndexOf('|');
            return idx >= 0 ? Name.Substring(idx + 1) : "";
        }
        set
        {
            Name = PortionName + "|" + value;
        }
    }
}

/// <summary>
/// The only screen a Restaurant owner ever fills in to define a menu item:
/// Name, Category, Selling Price, Active, an optional photo, and Save/Cancel
/// (<see cref="MasterDataEditFormBase"/>'s own OK/Cancel). No SKU, no
/// Variant, no Price List, no tax configuration - every one of those Catalog
/// concepts is resolved automatically by the caller
/// (<c>MenuItemsForm</c>) via the existing
/// <c>CreateProductWithPriceCommand</c>/granular Product-Variant-Price
/// commands, never surfaced here. Same single-file
/// <see cref="MasterDataEditFormBase"/> shape <c>ProductPriceEditForm</c>
/// already uses for a small, fixed field set. Control tree lives in
/// <c>MenuItemEditForm.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class MenuItemEditForm : MasterDataEditFormBase
{
    private const int PhotoBoxSize = 120;

    private readonly Dictionary<string, Guid?> _categoriesByDisplay;
    private readonly Func<string, Task<bool>>? _checkBarcodeExists;
    private Image? _pendingImage;
    private readonly BindingList<MenuItemVariantEditRow> _variantsList = [];

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public MenuItemEditForm() : base("Edit Menu Item")
    {
        _categoriesByDisplay = null!;

        InitializeComponent();
    }

    /// <summary>
    /// Builds the dialog. <paramref name="title"/> is the dialog's caption;
    /// <paramref name="categoryOptions"/> populates the category combo.
    /// <paramref name="name"/>, <paramref name="categoryId"/>,
    /// <paramref name="sellingPrice"/>, and <paramref name="isActive"/>
    /// pre-populate the fields when editing an existing menu item. Pass
    /// <paramref name="existingImage"/> (the item's current photo, if any -
    /// see <see cref="MenuItemImageStore"/>) when editing, so the picture
    /// editor starts populated instead of blank.
    /// </summary>
    public MenuItemEditForm(
        string title,
        IReadOnlyList<(Guid Id, string Display)> categoryOptions,
        string? name = null,
        Guid? categoryId = null,
        decimal sellingPrice = 0,
        bool isActive = true,
        Image? existingImage = null,
        string? barcode1 = null,
        string? barcode2 = null,
        string? barcode3 = null,
        Func<string, Task<bool>>? checkBarcodeExists = null,
        List<MenuItemVariantEditRow>? variants = null) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _categoriesByDisplay = null!;
            return;
        }

        _nameEdit.Text = name ?? string.Empty;
        _priceEdit.Value = sellingPrice;
        _activeEdit.Checked = isActive;
        _pictureEdit.Image = existingImage;
        _noPhotoLabel.Visible = existingImage is null;

        _barcode1Edit.Text = barcode1 ?? string.Empty;
        _barcode2Edit.Text = barcode2 ?? string.Empty;
        _barcode3Edit.Text = barcode3 ?? string.Empty;
        _checkBarcodeExists = checkBarcodeExists;

        _categoriesByDisplay = ComboBoxBinder.Bind(_categoryCombo, categoryOptions, includeEmpty: true);
        ComboBoxBinder.SelectById(_categoryCombo, _categoriesByDisplay, categoryId);

        _priceEdit.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        _priceEdit.Properties.Mask.EditMask = "F" + Clovent.Desktop.Forms.Base.CurrencyDisplay.DecimalPlaces;
        _priceEdit.Properties.Mask.UseMaskAsDisplayFormat = true;

        // Initialize and bind variants list
        if (variants != null && variants.Count > 0)
        {
            foreach (var v in variants)
            {
                _variantsList.Add(v);
            }

            // If we have custom variant names (not matching the product name or empty), turn on the toggle
            var hasCustomPortions = variants.Count > 1 || 
                                    (variants.Count == 1 && !string.Equals(variants[0].PortionName, name, StringComparison.OrdinalIgnoreCase));
            _hasVariantsEdit.Checked = hasCustomPortions;
        }
        else
        {
            _hasVariantsEdit.Checked = false;
        }

        _variantsGrid.DataSource = _variantsList;

        // Configure Grid Columns
        _variantsGridView.Columns.Clear();

        var colPortionName = _variantsGridView.Columns.AddVisible("PortionName", "Portion Name");
        colPortionName.VisibleIndex = 0;

        var colPosLabel = _variantsGridView.Columns.AddVisible("PosLabel", "POS Label");
        colPosLabel.VisibleIndex = 1;

        var colPrice = _variantsGridView.Columns.AddVisible("Price", "Selling Price");
        colPrice.VisibleIndex = 2;

        var colActive = _variantsGridView.Columns.AddVisible("IsActive", "Active");
        colActive.VisibleIndex = 3;

        var colB1 = _variantsGridView.Columns.AddVisible("Barcode1", "Barcode 1");
        colB1.VisibleIndex = 4;

        var colB2 = _variantsGridView.Columns.AddVisible("Barcode2", "Barcode 2");
        colB2.VisibleIndex = 5;

        var colB3 = _variantsGridView.Columns.AddVisible("Barcode3", "Barcode 3");
        colB3.VisibleIndex = 6;

        // Configure Price Editor with decimals
        var gridPriceEditor = new DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit();
        gridPriceEditor.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        gridPriceEditor.Mask.EditMask = "F" + Clovent.Desktop.Forms.Base.CurrencyDisplay.DecimalPlaces;
        gridPriceEditor.Mask.UseMaskAsDisplayFormat = true;
        gridPriceEditor.MinValue = 0.01m;
        gridPriceEditor.MaxValue = 1_000_000m;
        _variantsGrid.RepositoryItems.Add(gridPriceEditor);
        colPrice.ColumnEdit = gridPriceEditor;

        // Ensure newly added rows have unique Guid and are marked IsNew
        _variantsList.ListChanged += (s, ev) =>
        {
            if (ev.ListChangedType == ListChangedType.ItemAdded)
            {
                var item = _variantsList[ev.NewIndex];
                if (item.ProductVariantId == Guid.Empty)
                {
                    item.ProductVariantId = Guid.NewGuid();
                    item.IsNew = true;
                }
            }
        };

        _hasVariantsEdit.CheckedChanged += (s, ev) => ToggleVariantLayout();
        ToggleVariantLayout();
    }

    /// <summary>The entered item name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <summary>The entered barcode 1.</summary>
    public string Barcode1 => _barcode1Edit.Text.Trim();

    /// <summary>The entered barcode 2.</summary>
    public string Barcode2 => _barcode2Edit.Text.Trim();

    /// <summary>The entered barcode 3.</summary>
    public string Barcode3 => _barcode3Edit.Text.Trim();

    /// <summary>The selected category, or <see langword="null"/>.</summary>
    public Guid? CategoryId => ComboBoxBinder.GetSelectedId(_categoryCombo, _categoriesByDisplay);

    /// <summary>The entered selling price.</summary>
    public decimal SellingPrice => _priceEdit.Value;

    /// <summary>Whether the item should be sellable.</summary>
    public bool ItemIsActive => _activeEdit.Checked;

    /// <summary>Whether the dialog was closed via "Save &amp; New" - the caller (<c>MenuItemsForm</c>) reopens a fresh dialog for the next item when this is set.</summary>
    public bool IsSaveAndNew => SavedAndNew;

    /// <summary>A newly-chosen photo to persist, or <see langword="null"/> if the user didn't pick one this time (the existing photo, if any, is left untouched).</summary>
    public Image? PendingImage => _pendingImage;

    /// <summary>Whether the user explicitly cleared the photo (distinct from simply not choosing a new one).</summary>
    public bool ImageCleared { get; private set; }

    /// <summary>Whether the user wants multiple portions/variants.</summary>
    public bool HasVariants => _hasVariantsEdit.Checked;

    /// <summary>List of variants defined for the menu item.</summary>
    public List<MenuItemVariantEditRow> Variants => [.. _variantsList];

    private void ToggleVariantLayout()
    {
        bool hasVariants = _hasVariantsEdit.Checked;

        // Hide/show single price fields
        label3.Visible = !hasVariants;
        _priceEdit.Visible = !hasVariants;

        // Hide/show single barcode fields
        labelBarcodesHeader.Visible = !hasVariants;
        labelBarcode1.Visible = !hasVariants;
        _barcode1Edit.Visible = !hasVariants;
        labelBarcode2.Visible = !hasVariants;
        _barcode2Edit.Visible = !hasVariants;
        labelBarcode3.Visible = !hasVariants;
        _barcode3Edit.Visible = !hasVariants;

        // Hide/show variants grid
        _variantsGrid.Visible = hasVariants;

        if (hasVariants && _variantsList.Count == 0)
        {
            _variantsList.Add(new MenuItemVariantEditRow
            {
                ProductVariantId = Guid.NewGuid(),
                Name = "Regular Plate",
                Price = _priceEdit.Value > 0 ? _priceEdit.Value : 250m,
                IsNew = true
            });
        }
    }

    private void ChooseImageButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Photo files|*.png;*.jpg;*.jpeg;*.bmp;*.gif" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _pendingImage = Image.FromFile(dialog.FileName);
        ImageCleared = false;
        _pictureEdit.Image = _pendingImage;
        _noPhotoLabel.Visible = false;
    }

    private void ClearImageButton_Click(object? sender, EventArgs e)
    {
        _pendingImage = null;
        ImageCleared = true;
        _pictureEdit.Image = null;
        _noPhotoLabel.Visible = true;
        _noPhotoLabel.BringToFront();
    }

    /// <summary>
    /// The photo area is this dialog's one fixed-size element; everything
    /// else measures itself. Scale the preview box (and the heading/
    /// subtitle chrome and the two action buttons' matching minimums) by
    /// the dialog's actual DPI before the base class's Load-time size
    /// recompute runs (base.OnLoad raises the Load event afterwards), so
    /// the dialog sizes itself to fit at every DPI.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        if (!Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            var factor = DeviceDpi / 96f;
            _headingLabel.Height = (int)Math.Round(40 * factor);
            _subtitleLabel.Height = (int)Math.Round(22 * factor);
            var boxSize = (int)Math.Round(PhotoBoxSize * factor);
            _photoBox.Size = new Size(boxSize, boxSize);
            _pictureEdit.Size = new Size(boxSize, boxSize);
            _noPhotoLabel.Size = new Size(boxSize, boxSize);
            var buttonMinWidth = (int)Math.Round(110 * factor);
            _chooseImageButton.MinimumSize = new Size(buttonMinWidth, 0);
            _clearImageButton.MinimumSize = new Size(buttonMinWidth, 0);
        }

        base.OnLoad(e);
    }

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Please enter a name for this menu item.";
            return false;
        }

        if (!_hasVariantsEdit.Checked)
        {
            if (_priceEdit.Value <= 0)
            {
                error = "Enter a selling price greater than 0.";
                return false;
            }

            var barcodes = new List<(string Name, string Value)>
            {
                ("Barcode 1", Barcode1),
                ("Barcode 2", Barcode2),
                ("Barcode 3", Barcode3)
            };

            var activeBarcodes = barcodes.Where(b => !string.IsNullOrEmpty(b.Value)).ToList();
            
            // 1. Format/Length checking
            foreach (var (name, value) in activeBarcodes)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(value, "^[0-9]{8,14}$"))
                {
                    error = $"{name} must contain only digits (8 to 14 digits).";
                    return false;
                }
            }

            // 2. Uniqueness among themselves
            for (int i = 0; i < activeBarcodes.Count; i++)
            {
                for (int j = i + 1; j < activeBarcodes.Count; j++)
                {
                    if (activeBarcodes[i].Value == activeBarcodes[j].Value)
                    {
                        error = $"{activeBarcodes[i].Name} and {activeBarcodes[j].Name} cannot have the same barcode value.";
                        return false;
                    }
                }
            }

            // 3. Global uniqueness check
            if (_checkBarcodeExists is not null)
            {
                foreach (var (name, value) in activeBarcodes)
                {
                    var exists = Task.Run(async () => await _checkBarcodeExists(value)).GetAwaiter().GetResult();
                    if (exists)
                    {
                        error = $"Barcode '{value}' is already in use by another item.";
                        return false;
                    }
                }
            }
        }
        else
        {
            if (_variantsList.Count == 0)
            {
                error = "Please add at least one portion/variant.";
                return false;
            }

            var activeBarcodes = new List<(string Portion, string BarcodeColumn, string Value)>();

            foreach (var variant in _variantsList)
            {
                if (string.IsNullOrWhiteSpace(variant.Name))
                {
                    error = "Portion Name cannot be empty.";
                    return false;
                }

                if (variant.Price <= 0)
                {
                    error = $"Portion '{variant.Name}' must have a selling price greater than 0.";
                    return false;
                }

                var bList = new[] {
                    ("Barcode 1", variant.Barcode1),
                    ("Barcode 2", variant.Barcode2),
                    ("Barcode 3", variant.Barcode3)
                };

                foreach (var (bName, bVal) in bList)
                {
                    if (!string.IsNullOrEmpty(bVal))
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(bVal, "^[0-9]{8,14}$"))
                        {
                            error = $"Portion '{variant.Name}' {bName} must contain only digits (8 to 14 digits).";
                            return false;
                        }
                        activeBarcodes.Add((variant.Name, bName, bVal));
                    }
                }
            }

            // Internal uniqueness check across all variants
            for (int i = 0; i < activeBarcodes.Count; i++)
            {
                for (int j = i + 1; j < activeBarcodes.Count; j++)
                {
                    if (activeBarcodes[i].Value == activeBarcodes[j].Value)
                    {
                        error = $"Portion '{activeBarcodes[i].Portion}' ({activeBarcodes[i].BarcodeColumn}) and Portion '{activeBarcodes[j].Portion}' ({activeBarcodes[j].BarcodeColumn}) cannot have the same barcode value '{activeBarcodes[i].Value}'.";
                        return false;
                    }
                }
            }

            // Global uniqueness check
            if (_checkBarcodeExists is not null)
            {
                foreach (var (portion, bName, value) in activeBarcodes)
                {
                    var exists = Task.Run(async () => await _checkBarcodeExists(value)).GetAwaiter().GetResult();
                    if (exists)
                    {
                        error = $"Barcode '{value}' used by Portion '{portion}' is already in use by another item.";
                        return false;
                    }
                }
            }
        }

        error = string.Empty;
        return true;
    }
}
