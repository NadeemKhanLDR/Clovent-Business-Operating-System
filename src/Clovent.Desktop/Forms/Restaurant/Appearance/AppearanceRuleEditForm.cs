using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Forms.Restaurant.Appearance;

/// <summary>
/// Create/edit dialog for one <see cref="AppearanceRule"/> - scope
/// (System/Module/Form/ControlType/Control, plus a key naming which one),
/// then an opt-in font override and opt-in text/background color overrides.
/// Each override group is gated by its own checkbox (the same "checkbox
/// enables the editor next to it" pattern <c>Forms.Restaurant.MenuItems.CategoryColorDialog</c>
/// already established) rather than forcing an owner to specify every field
/// just to change one - a rule that only sets a color doesn't also silently
/// force a font family/size on every matching control. Control tree lives in
/// <c>AppearanceRuleEditForm.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class AppearanceRuleEditForm : MasterDataEditFormBase
{
    // Suggested (not enforced - the combo stays editable) scope keys for
    // each scope type, covering every screen/control this pass's own
    // Restaurant module work named - an owner can still type any other
    // screen/control name CBOS-wide.
    private static readonly string[] ModuleSuggestions = ["Restaurant", "Catalog", "Inventory", "Identity", "MasterData"];
    private static readonly string[] FormSuggestions =
    [
        "RestaurantPosView", "PaymentPanel", "MenuItemsForm", "RestaurantSetupView",
        "PaymentMethodsView", "ActivityLogView", "EndOfDayReportView", "ReceiptPreviewForm",
    ];
    private static readonly string[] ControlTypeSuggestions =
    [
        "SimpleButton", "LabelControl", "TextEdit", "SpinEdit", "ComboBoxEdit", "SearchControl",
        "PanelControl", "GridView.HeaderPanel", "GridView.Row", "RibbonControl",
    ];
    private static readonly string[] ControlSuggestions =
    [
        "RestaurantPosView.TableLabel", "RestaurantPosView.CurrentBillLabel", "RestaurantPosView.GrandTotalLabel",
        "RestaurantPosView.PayButton", "RestaurantPosView.SearchBox", "RestaurantPosView.CategoryButton",
        "RestaurantPosView.ProductTileName", "RestaurantPosView.ProductTilePrice",
        "PaymentPanel.PaymentButton", "PaymentPanel.ReceiptButton",
    ];

    private readonly Guid _ruleId;

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public AppearanceRuleEditForm() : base("Edit Appearance Rule")
    {
        InitializeComponent();
        InitializeFontFamilyCombo();
    }

    /// <summary>Builds the dialog, optionally pre-filled from <paramref name="existing"/> (edit) or blank (create).</summary>
    public AppearanceRuleEditForm(string title, AppearanceRule? existing = null) : base(title)
    {
        InitializeComponent();
        InitializeFontFamilyCombo();

        _ruleId = existing?.RuleId ?? Guid.NewGuid();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _scopeTypeCombo.SelectedIndex = existing is null ? 0 : (int)existing.ScopeType;

        UpdateScopeKeySuggestions();

        if (existing is not null)
        {
            _scopeKeyCombo.Text = existing.ScopeKey;

            _overrideFontCheck.Checked = existing.FontFamily is not null || existing.FontSize is not null || existing.Bold is not null || existing.Italic is not null || existing.Underline is not null || existing.Strikeout is not null;
            if (existing.FontFamily is not null) _fontFamilyCombo.Text = existing.FontFamily;
            if (existing.FontSize is { } size) _fontSizeEdit.Value = (decimal)size;
            _boldCheck.Checked = existing.Bold ?? false;
            _italicCheck.Checked = existing.Italic ?? false;
            _underlineCheck.Checked = existing.Underline ?? false;
            _strikeoutCheck.Checked = existing.Strikeout ?? false;

            _overrideForeColorCheck.Checked = existing.ForeColorHex is not null;
            if (existing.ForeColorHex is not null) _foreColorEdit.Color = ColorTranslator.FromHtml(existing.ForeColorHex);

            _overrideBackColorCheck.Checked = existing.BackColorHex is not null;
            if (existing.BackColorHex is not null) _backColorEdit.Color = ColorTranslator.FromHtml(existing.BackColorHex);
        }

        UpdateFontFieldsEnabled();
        _foreColorEdit.Enabled = _overrideForeColorCheck.Checked;
        _backColorEdit.Enabled = _overrideBackColorCheck.Checked;
    }

    private void ScopeTypeCombo_EditValueChanged(object? sender, EventArgs e) => UpdateScopeKeySuggestions();

    private void OverrideFontCheck_CheckedChanged(object? sender, EventArgs e) => UpdateFontFieldsEnabled();

    private void OverrideForeColorCheck_CheckedChanged(object? sender, EventArgs e) => _foreColorEdit.Enabled = _overrideForeColorCheck.Checked;

    private void OverrideBackColorCheck_CheckedChanged(object? sender, EventArgs e) => _backColorEdit.Enabled = _overrideBackColorCheck.Checked;

    private void UpdateFontFieldsEnabled()
    {
        var enabled = _overrideFontCheck.Checked;
        _fontFamilyCombo.Enabled = enabled;
        _fontSizeEdit.Enabled = enabled;
        _boldCheck.Enabled = enabled;
        _italicCheck.Enabled = enabled;
        _underlineCheck.Enabled = enabled;
        _strikeoutCheck.Enabled = enabled;
    }

    private void UpdateScopeKeySuggestions()
    {
        var scopeType = SelectedScopeType;
        var suggestions = scopeType switch
        {
            AppearanceScopeType.Module => ModuleSuggestions,
            AppearanceScopeType.Form => FormSuggestions,
            AppearanceScopeType.ControlType => ControlTypeSuggestions,
            AppearanceScopeType.Control => ControlSuggestions,
            _ => [],
        };

        _scopeKeyCombo.Properties.Items.Clear();
        _scopeKeyCombo.Properties.Items.AddRange(suggestions);
        _scopeKeyCombo.Enabled = scopeType != AppearanceScopeType.System;
        if (scopeType == AppearanceScopeType.System)
        {
            _scopeKeyCombo.Text = string.Empty;
        }
    }

    private AppearanceScopeType SelectedScopeType => Enum.Parse<AppearanceScopeType>((string)_scopeTypeCombo.SelectedItem);

    /// <summary>Builds the <see cref="AppearanceRule"/> this dialog currently describes.</summary>
    public AppearanceRule BuildRule() => new(
        _ruleId,
        SelectedScopeType,
        SelectedScopeType == AppearanceScopeType.System ? string.Empty : _scopeKeyCombo.Text.Trim(),
        _overrideFontCheck.Checked ? _fontFamilyCombo.Text.Trim() : null,
        _overrideFontCheck.Checked ? (float)_fontSizeEdit.Value : null,
        _overrideFontCheck.Checked ? _boldCheck.Checked : null,
        _overrideFontCheck.Checked ? _italicCheck.Checked : null,
        _overrideFontCheck.Checked ? _underlineCheck.Checked : null,
        _overrideFontCheck.Checked ? _strikeoutCheck.Checked : null,
        _overrideForeColorCheck.Checked ? ColorToHex(_foreColorEdit.Color) : null,
        _overrideBackColorCheck.Checked ? ColorToHex(_backColorEdit.Color) : null);

    private static string ColorToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (SelectedScopeType != AppearanceScopeType.System && string.IsNullOrWhiteSpace(_scopeKeyCombo.Text))
        {
            error = "Enter or choose a scope key (which module/screen/control type/control this rule applies to).";
            return false;
        }

        if (!_overrideFontCheck.Checked && !_overrideForeColorCheck.Checked && !_overrideBackColorCheck.Checked)
        {
            error = "Enable at least one override (font, text color, or background color) - a rule that overrides nothing has no effect.";
            return false;
        }

        if (_overrideFontCheck.Checked && string.IsNullOrWhiteSpace(_fontFamilyCombo.Text))
        {
            error = "Choose a font family, or turn off \"Override font\".";
            return false;
        }

        error = string.Empty;
        return true;
     }



    private void InitializeFontFamilyCombo()
    {
        try
        {
            _fontFamilyCombo.Properties.Items.Clear();
            foreach (var family in FontFamily.Families)
            {
                _fontFamilyCombo.Properties.Items.Add(family.Name);
            }
        }
        catch
        {
            _fontFamilyCombo.Properties.Items.Add("Segoe UI");
            _fontFamilyCombo.Properties.Items.Add("Arial");
            _fontFamilyCombo.Properties.Items.Add("Tahoma");
        }
    }
}
