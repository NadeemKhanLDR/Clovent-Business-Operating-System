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
        Image? existingImage = null) : base(title)
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

        _categoriesByDisplay = ComboBoxBinder.Bind(_categoryCombo, categoryOptions, includeEmpty: true);
        ComboBoxBinder.SelectById(_categoryCombo, _categoriesByDisplay, categoryId);
    }
    /// <summary>The entered item name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

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

        error = string.Empty;
        return true;
    }

    }
