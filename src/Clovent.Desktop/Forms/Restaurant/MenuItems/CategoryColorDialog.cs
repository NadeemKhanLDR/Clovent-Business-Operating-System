using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Forms.Restaurant.MenuItems;

/// <summary>
/// Prompt for a category's display color - the POS category rail's optional
/// color-coding (<c>RestaurantPosView.BuildCategoryButtons</c>). "Clear
/// Color" resets it back to the screen's default rail color. Control tree
/// lives in <c>CategoryColorDialog.Designer.cs</c>; this file holds behavior
/// only.
/// </summary>
public sealed partial class CategoryColorDialog : MasterDataEditFormBase
{
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CategoryColorDialog() : base("Category Color")
    {
        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog for <paramref name="categoryName"/>, currently
    /// colored <paramref name="currentColorHex"/> (or uncolored, if
    /// <see langword="null"/>).
    /// </summary>
    public CategoryColorDialog(string categoryName, string? currentColorHex) : base($"Category Color - {categoryName}")
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _colorEdit.Color = currentColorHex is not null
            ? ColorTranslator.FromHtml(currentColorHex)
            : Color.FromArgb(37, 99, 235);
    }

    /// <summary>The chosen color as a "#RRGGBB" hex string, or <see langword="null"/> if "Clear Color" was checked.</summary>
    public string? ColorHex => _clearCheck.Checked
        ? null
        : $"#{_colorEdit.Color.R:X2}{_colorEdit.Color.G:X2}{_colorEdit.Color.B:X2}";

    private void ClearCheck_CheckedChanged(object? sender, EventArgs e) => _colorEdit.Enabled = !_clearCheck.Checked;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        error = string.Empty;
        return true;
    }

    }
