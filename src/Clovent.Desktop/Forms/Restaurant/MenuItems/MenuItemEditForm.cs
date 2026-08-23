using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Forms.Restaurant.MenuItems;

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
        Func<string, Task<bool>>? checkBarcodeExists = null) : base(title)
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

        error = string.Empty;
        return true;
    }

    }
