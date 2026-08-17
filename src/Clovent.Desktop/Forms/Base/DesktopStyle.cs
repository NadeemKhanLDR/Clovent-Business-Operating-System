namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Shared layout/typography constants every screen's <c>.Designer.cs</c>
/// should reference instead of re-picking its own numbers - this is what
/// makes every screen "look like it belongs to the same ERP" (consistent
/// toolbar height, button size, spacing, fonts) without a runtime style
/// engine. Referencing these from hand-authored Designer.cs code is safe:
/// they are plain static fields/constants, resolved like any other
/// constant a VS-Designer-generated file could reference.
/// </summary>
public static class DesktopStyle
{
    /// <summary>Standard gap between adjacent controls in a toolbar/flow layout.</summary>
    public const int ControlGap = 8;

    /// <summary>Standard outer padding for panels/dialogs.</summary>
    public const int PanelPadding = 8;

    /// <summary>Default toolbar band height (before <see cref="BaseForm"/>'s live <c>ToolbarFlow</c> resize adjusts it).</summary>
    public const int ToolbarHeight = 44;

    /// <summary>Standard height for a toolbar <c>SimpleButton</c>/<c>TextEdit</c>.</summary>
    public const int ToolbarControlHeight = 30;

    /// <summary>Standard width for a short toolbar button ("New", "Edit", "Refresh").</summary>
    public const int ButtonWidthSmall = 80;

    /// <summary>Standard width for a medium toolbar button ("Activate", "Export CSV").</summary>
    public const int ButtonWidthMedium = 100;

    /// <summary>Standard width for a wide toolbar button ("Reset Password").</summary>
    public const int ButtonWidthLarge = 130;

    /// <summary>Standard width for a toolbar search box.</summary>
    public const int SearchBoxWidth = 220;

    /// <summary>Standard gap between KPI/summary cards in a card grid.</summary>
    public const int CardGap = 12;

    /// <summary>Standard height for a KPI/summary card.</summary>
    public const int CardHeight = 110;

    /// <summary>Page title font ("Dashboard", etc.).</summary>
    public static Font PageTitleFont { get; } = new("Segoe UI", 18F, FontStyle.Bold);

    /// <summary>Section heading font ("Recent Activity", "KPI card values").</summary>
    public static Font SectionHeadingFont { get; } = new("Segoe UI", 11F, FontStyle.Bold);

    /// <summary>KPI card value font (the big number).</summary>
    public static Font CardValueFont { get; } = new("Segoe UI", 20F, FontStyle.Bold);

    /// <summary>Caption/label font for field captions and card titles.</summary>
    public static Font CaptionFont { get; } = new("Segoe UI", 9F);

    /// <summary>Muted foreground color for secondary captions (e.g. "Current Organization" above its value).</summary>
    public static Color CaptionForeColor { get; } = Color.Gray;
}
