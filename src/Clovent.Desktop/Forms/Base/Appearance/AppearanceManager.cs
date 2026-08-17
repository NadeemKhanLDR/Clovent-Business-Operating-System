using System.Reflection;
using DevExpress.Utils;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Forms.Base.Appearance;

/// <summary>
/// Applies owner-configured font/color rules (<see cref="AppearanceRule"/>,
/// managed from the Restaurant Setup -&gt; Appearance screen) to a control
/// tree - the "Font Engine" the design brief asks for, so no screen ever
/// hand-sets a font/color that should instead come from configuration.
/// Loaded once and cached (<see cref="Rules"/>); a screen calls
/// <see cref="Apply(Control, string, string)"/> once after building its own
/// controls (mirroring how every screen already calls its own
/// <c>LoadAsync</c>/<c>RefreshAsync</c> once after construction), and
/// subscribes to <see cref="Changed"/> to re-apply when the owner saves new
/// settings - see each Restaurant screen's own <c>AppearanceManager_Changed</c>
/// handler for the "live update, no restart" wiring.
/// </summary>
public static class AppearanceManager
{
    private static IReadOnlyList<AppearanceRule> _rules = AppearanceSettingsStore.Load();

    /// <summary>Raised after <see cref="Save"/> persists a new rule set - screens already showing re-apply from this, rather than needing a restart.</summary>
    public static event EventHandler? Changed;

    /// <summary>The currently cached rule set - loaded once at process start (or the last <see cref="Save"/>), never re-read from disk on every repaint.</summary>
    public static IReadOnlyList<AppearanceRule> Rules => _rules;

    /// <summary>Persists <paramref name="rules"/> as the new full rule set, refreshes the in-memory cache, and raises <see cref="Changed"/> so every open screen re-applies immediately.</summary>
    public static void Save(IReadOnlyList<AppearanceRule> rules)
    {
        AppearanceSettingsStore.Save(rules);
        _rules = rules;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Applies the best-matching rule (see <see cref="AppearanceScopeType"/>'s
    /// precedence) to <paramref name="root"/> and every descendant control,
    /// recursively - including grid header/row appearance for any
    /// <see cref="GridControl"/> encountered. <paramref name="moduleName"/>
    /// (e.g. "Restaurant") and <paramref name="formTypeName"/> (the calling
    /// screen's own <c>GetType().Name</c>, e.g. "RestaurantPosView") scope
    /// <see cref="AppearanceScopeType.Module"/>/<see cref="AppearanceScopeType.Form"/>/
    /// <see cref="AppearanceScopeType.Control"/> rule matching.
    /// </summary>
    public static void Apply(Control root, string moduleName, string formTypeName)
    {
        ApplyToControl(root, moduleName, formTypeName);

        foreach (Control child in root.Controls)
        {
            Apply(child, moduleName, formTypeName);
        }

        if (root is GridControl gridControl)
        {
            foreach (var view in gridControl.ViewCollection)
            {
                if (view is GridView gridView)
                {
                    ApplyRule(ResolveRule(moduleName, formTypeName, "GridView.HeaderPanel", null), gridView.Appearance.HeaderPanel);
                    ApplyRule(ResolveRule(moduleName, formTypeName, "GridView.Row", null), gridView.Appearance.Row);
                }
            }
        }
    }

    private static void ApplyToControl(Control control, string moduleName, string formTypeName)
    {
        var rule = ResolveRule(moduleName, formTypeName, control.GetType().Name, string.IsNullOrEmpty(control.Name) ? null : control.Name);
        if (rule is null)
        {
            return;
        }

        // DevExpress controls (SimpleButton/LabelControl/PanelControl/...)
        // expose a public "Appearance" (DevExpress.Utils.AppearanceObject)
        // property that the active skin also draws from - setting a plain
        // WinForms Control.Font/.ForeColor/.BackColor on one of these is
        // usually silently overridden by the skin's own paint code, so this
        // targets Appearance whenever a control offers one. Editors
        // (TextEdit/SpinEdit/ComboBoxEdit/...) don't expose Appearance
        // directly - theirs lives one level down, on Properties (a
        // RepositoryItem), which is why that path is tried second. Falls
        // back to plain WinForms properties only when neither exists (a
        // bare Panel/FlowLayoutPanel/TextBox/...).
        if (GetPropertySafe(control, "Appearance") is AppearanceObject directAppearance)
        {
            ApplyRule(rule, directAppearance);
        }
        else if (GetPropertySafe(control, "Properties") is { } editorProperties && GetPropertySafe(editorProperties, "Appearance") is AppearanceObject editorAppearance)
        {
            ApplyRule(rule, editorAppearance);
        }
        else
        {
            ApplyRuleToPlainControl(rule, control);
        }
    }

    /// <summary>
    /// Reflection-based property read that never throws
    /// <see cref="AmbiguousMatchException"/> - several DevExpress editor
    /// types (e.g. <c>TextEdit.Properties</c> shadowing <c>BaseEdit.Properties</c>
    /// with a narrower covariant return type) declare more than one property
    /// with the same name at different levels of their inheritance chain,
    /// which the single-property overload of <see cref="Type.GetProperty(string)"/>
    /// treats as an ambiguous match and throws on - confirmed by reflecting
    /// over the installed DevExpress assemblies rather than assumed, since
    /// this exact shape isn't documented inline. Prefers the most-derived
    /// declaration (closest to the control's actual runtime type).
    /// </summary>
    private static object? GetPropertySafe(object instance, string propertyName)
    {
        var candidate = instance.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name == propertyName && p.GetIndexParameters().Length == 0)
            .OrderByDescending(InheritanceDepth)
            .FirstOrDefault();

        try
        {
            return candidate?.GetValue(instance);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }

    private static int InheritanceDepth(PropertyInfo property)
    {
        var depth = 0;
        var type = property.DeclaringType;
        while (type is not null)
        {
            depth++;
            type = type.BaseType;
        }

        return depth;
    }

    /// <summary>Resolves the single best-matching rule for a control, most specific first: Control &gt; ControlType &gt; Form &gt; Module &gt; System.</summary>
    private static AppearanceRule? ResolveRule(string moduleName, string formTypeName, string controlTypeName, string? controlName)
    {
        if (controlName is not null)
        {
            var controlKey = $"{formTypeName}.{controlName}";
            if (FindRule(AppearanceScopeType.Control, controlKey) is { } controlRule)
            {
                return controlRule;
            }
        }

        return FindRule(AppearanceScopeType.ControlType, controlTypeName)
            ?? FindRule(AppearanceScopeType.Form, formTypeName)
            ?? FindRule(AppearanceScopeType.Module, moduleName)
            ?? FindRule(AppearanceScopeType.System, string.Empty);
    }

    private static AppearanceRule? FindRule(AppearanceScopeType scopeType, string scopeKey) =>
        _rules.FirstOrDefault(r => r.ScopeType == scopeType && (scopeType == AppearanceScopeType.System || string.Equals(r.ScopeKey, scopeKey, StringComparison.OrdinalIgnoreCase)));

    private static void ApplyRule(AppearanceRule? rule, AppearanceObject appearance)
    {
        if (rule is null)
        {
            return;
        }

        if (rule.FontFamily is not null || rule.FontSize is not null || rule.Bold is not null || rule.Italic is not null || rule.Underline is not null || rule.Strikeout is not null)
        {
            var baseFont = appearance.Font;
            var family = rule.FontFamily ?? baseFont.FontFamily.Name;
            var size = rule.FontSize ?? baseFont.Size;
            var style = ResolveStyle(baseFont.Style, rule);

            appearance.Font = new Font(family, size, style);
            appearance.Options.UseFont = true;
        }

        if (ParseColor(rule.ForeColorHex) is { } foreColor)
        {
            appearance.ForeColor = foreColor;
            appearance.Options.UseForeColor = true;
        }

        if (ParseColor(rule.BackColorHex) is { } backColor)
        {
            appearance.BackColor = backColor;
            appearance.Options.UseBackColor = true;
        }
    }

    private static void ApplyRuleToPlainControl(AppearanceRule rule, Control control)
    {
        if (rule.FontFamily is not null || rule.FontSize is not null || rule.Bold is not null || rule.Italic is not null || rule.Underline is not null || rule.Strikeout is not null)
        {
            var family = rule.FontFamily ?? control.Font.FontFamily.Name;
            var size = rule.FontSize ?? control.Font.Size;
            var style = ResolveStyle(control.Font.Style, rule);

            control.Font = new Font(family, size, style);
        }

        if (ParseColor(rule.ForeColorHex) is { } foreColor)
        {
            control.ForeColor = foreColor;
        }

        if (ParseColor(rule.BackColorHex) is { } backColor)
        {
            control.BackColor = backColor;
        }
    }

    private static FontStyle ResolveStyle(FontStyle current, AppearanceRule rule)
    {
        var style = FontStyle.Regular;
        if (rule.Bold ?? current.HasFlag(FontStyle.Bold))
        {
            style |= FontStyle.Bold;
        }
        if (rule.Italic ?? current.HasFlag(FontStyle.Italic))
        {
            style |= FontStyle.Italic;
        }
        if (rule.Underline ?? current.HasFlag(FontStyle.Underline))
        {
            style |= FontStyle.Underline;
        }
        if (rule.Strikeout ?? current.HasFlag(FontStyle.Strikeout))
        {
            style |= FontStyle.Strikeout;
        }

        return style;
    }

    private static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return null;
        }

        try
        {
            return ColorTranslator.FromHtml(hex);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return null;
        }
    }
}
