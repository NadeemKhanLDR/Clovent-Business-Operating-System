using System.Drawing;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.Categories;

/// <summary>
/// Create/edit dialog for a Product Category - name and an optional parent
/// category (for the "Beverages -> Soft Drinks" hierarchy) plus display color. Control tree
/// (fields, <c>AddField</c> calls) lives in
/// <c>ProductCategoryEditForm.Designer.cs</c>; this file holds behavior
/// only.
/// </summary>
public sealed partial class ProductCategoryEditForm : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid?> _parentsByDisplay;

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ProductCategoryEditForm() : base("Edit Category")
    {
        _parentsByDisplay = null!;

        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog. <paramref name="title"/> is the dialog's caption.
    /// <paramref name="parentOptions"/> should exclude the category being
    /// edited, if any, to prevent a self-parent. <paramref name="name"/> and
    /// <paramref name="parentCategoryId"/> pre-populate the fields when
    /// editing an existing category; both left <see langword="null"/> when
    /// creating a new one.
    /// </summary>
    public ProductCategoryEditForm(
        string title,
        IReadOnlyList<(Guid Id, string Display)> parentOptions,
        string? name = null,
        Guid? parentCategoryId = null,
        string? currentColorHex = null) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _parentsByDisplay = null!;
            return;
        }

        _nameEdit.Text = name ?? string.Empty;
        _parentsByDisplay = ComboBoxBinder.Bind(_parentCombo, parentOptions, includeEmpty: true);
        ComboBoxBinder.SelectById(_parentCombo, _parentsByDisplay, parentCategoryId);

        _clearColorCheck.Checked = currentColorHex is null;
        _colorEdit.Enabled = currentColorHex is not null;
        _colorEdit.Color = currentColorHex is not null
            ? ColorTranslator.FromHtml(currentColorHex)
            : Color.FromArgb(37, 99, 235);

        _clearColorCheck.CheckedChanged += (s, e) => _colorEdit.Enabled = !_clearColorCheck.Checked;
    }

    /// <summary>The entered category name.</summary>
    public string NameValue => _nameEdit.Text.Trim();
    /// <summary>The selected parent category, or <see langword="null"/> for a top-level category.</summary>
    public Guid? ParentCategoryId => ComboBoxBinder.GetSelectedId(_parentCombo, _parentsByDisplay);
    /// <summary>The chosen color as a "#RRGGBB" hex string, or <see langword="null"/> if cleared/defaulted.</summary>
    public string? ColorHex => _clearColorCheck.Checked
        ? null
        : $"#{_colorEdit.Color.R:X2}{_colorEdit.Color.G:X2}{_colorEdit.Color.B:X2}";

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
