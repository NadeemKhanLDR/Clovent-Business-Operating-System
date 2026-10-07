namespace Clovent.Desktop.Forms.Base;

using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;

/// <summary>
/// Authoritative typography definitions and shared layout constants for the CBOS Desktop application.
/// Provides centralized semantic fonts and global styling appliers to prevent font fragmentation
/// and ensure consistent, readable scaling across high-DPI displays (including 200%–250% scaling).
/// Referencing these from hand-authored Designer.cs code is safe: they are plain static fields/constants,
/// resolved like any other constant a VS-Designer-generated file can reference.
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

    /// <summary>Page header font ("Dashboard", "First-Run Commissioning", etc.).</summary>
    public static Font PageHeaderFont { get; } = new("Segoe UI", 16F, FontStyle.Bold);

    /// <summary>Backward-compatible alias for <see cref="PageHeaderFont"/>.</summary>
    public static Font PageTitleFont => PageHeaderFont;

    /// <summary>Section header font ("Recent Activity", KPI section headings, card group titles).</summary>
    public static Font SectionHeaderFont { get; } = new("Segoe UI", 11.5F, FontStyle.Bold);

    /// <summary>Backward-compatible alias for <see cref="SectionHeaderFont"/>.</summary>
    public static Font SectionHeadingFont => SectionHeaderFont;

    /// <summary>KPI card primary value font (the big number/metric display).</summary>
    public static Font CardValueFont { get; } = new("Segoe UI", 20F, FontStyle.Bold);

    /// <summary>Standard readable body font for textboxes, lists, informational panels, and dialog content (10pt).</summary>
    public static Font BodyFont { get; } = new("Segoe UI", 10F, FontStyle.Regular);

    /// <summary>Bold variant of standard readable body font for emphasized items and summary lines.</summary>
    public static Font BodyFontBold { get; } = new("Segoe UI", 10F, FontStyle.Bold);

    /// <summary>Standard button font for command buttons (10pt).</summary>
    public static Font ButtonFont { get; } = new("Segoe UI", 10F, FontStyle.Regular);

    /// <summary>Bold button font for primary/accent action buttons (10pt Bold).</summary>
    public static Font ButtonFontBold { get; } = new("Segoe UI", 10F, FontStyle.Bold);

    /// <summary>Navigation font for Ribbon tabs, navigation trees, and top-level menus (9.5pt).</summary>
    public static Font NavigationFont { get; } = new("Segoe UI", 9.5F, FontStyle.Regular);

    /// <summary>Ribbon item font for Ribbon buttons and bar items (9.5pt).</summary>
    public static Font RibbonFont { get; } = new("Segoe UI", 9.5F, FontStyle.Regular);

    /// <summary>Small Ribbon font for group captions and secondary descriptions (9pt).</summary>
    public static Font RibbonSmallFont { get; } = new("Segoe UI", 9F, FontStyle.Regular);

    /// <summary>Standard grid row, cell, and group row font (9.5pt).</summary>
    public static Font GridFont { get; } = new("Segoe UI", 9.5F, FontStyle.Regular);

    /// <summary>Standard grid column header panel font (10pt Bold).</summary>
    public static Font GridHeaderFont { get; } = new("Segoe UI", 10F, FontStyle.Bold);

    /// <summary>Caption/label font for field captions and card titles (9pt).</summary>
    public static Font CaptionFont { get; } = new("Segoe UI", 9F, FontStyle.Regular);

    /// <summary>Small caption font for secondary footnotes and compact hints (9pt).</summary>
    public static Font SmallCaptionFont { get; } = new("Segoe UI", 9F, FontStyle.Regular);

    /// <summary>Status font for status bars, footer indicators, and sync badges (9.5pt).</summary>
    public static Font StatusFont { get; } = new("Segoe UI", 9.5F, FontStyle.Regular);

    /// <summary>Muted foreground color for secondary captions (e.g. "Current Organization" above its value).</summary>
    public static Color CaptionForeColor { get; } = Color.Gray;

    /// <summary>
    /// Applies the centralized CBOS typography globally across both WinForms and DevExpress runtime environments.
    /// Must be invoked early during application startup in <c>Program.Main()</c> before any forms are created.
    /// Sets WinForms <see cref="Application.SetDefaultFont"/>, DevExpress <see cref="WindowsFormsSettings.DefaultFont"/>,
    /// <see cref="WindowsFormsSettings.DefaultMenuFont"/>, and central Ribbon/Bar controller typography.
    /// </summary>
    public static void ApplyGlobalTypography()
    {
        try
        {
            Application.SetDefaultFont(BodyFont);
        }
        catch (InvalidOperationException)
        {
            // SetDefaultFont throws if invoked after any control/form has been created; safe to ignore.
        }

        WindowsFormsSettings.DefaultFont = BodyFont;
        WindowsFormsSettings.DefaultMenuFont = NavigationFont;

        ApplyRibbonControllerTypography(BarAndDockingController.Default);
    }

    /// <summary>
    /// Configures central typography on a <see cref="BarAndDockingController"/> to ensure Ribbon tabs,
    /// button captions, group captions, and status bars render at readable high-DPI font sizes.
    /// </summary>
    public static void ApplyRibbonControllerTypography(BarAndDockingController? controller)
    {
        controller ??= BarAndDockingController.Default;

        // Ribbon Tabs (Page Headers)
        controller.AppearancesRibbon.PageHeader.Font = NavigationFont;
        controller.AppearancesRibbon.PageHeader.Options.UseFont = true;
        controller.AppearancesRibbon.PageHeaderSelected.Font = NavigationFont;
        controller.AppearancesRibbon.PageHeaderSelected.Options.UseFont = true;
        controller.AppearancesRibbon.PageHeaderHovered.Font = NavigationFont;
        controller.AppearancesRibbon.PageHeaderHovered.Options.UseFont = true;

        // Ribbon Items (Button captions)
        controller.AppearancesRibbon.Item.Font = RibbonFont;
        controller.AppearancesRibbon.Item.Options.UseFont = true;
        controller.AppearancesRibbon.ItemHovered.Font = RibbonFont;
        controller.AppearancesRibbon.ItemHovered.Options.UseFont = true;
        controller.AppearancesRibbon.ItemPressed.Font = RibbonFont;
        controller.AppearancesRibbon.ItemPressed.Options.UseFont = true;

        // Ribbon Page Group Captions (text beneath button icons)
        controller.AppearancesRibbon.PageGroupCaption.Font = RibbonSmallFont;
        controller.AppearancesRibbon.PageGroupCaption.Options.UseFont = true;

        // Secondary item descriptions
        controller.AppearancesRibbon.ItemDescription.Font = RibbonSmallFont;
        controller.AppearancesRibbon.ItemDescription.Options.UseFont = true;
        controller.AppearancesRibbon.ItemDescriptionHovered.Font = RibbonSmallFont;
        controller.AppearancesRibbon.ItemDescriptionHovered.Options.UseFont = true;

        // Status bars and generic bar items
        controller.AppearancesBar.Bar.Font = StatusFont;
        controller.AppearancesBar.Bar.Options.UseFont = true;
        controller.AppearancesBar.StatusBar.Font = StatusFont;
        controller.AppearancesBar.StatusBar.Options.UseFont = true;
        controller.AppearancesBar.ItemsFont = StatusFont;
    }

    /// <summary>
    /// Configures typography for a specific <see cref="RibbonControl"/> and optional <see cref="RibbonStatusBar"/>.
    /// </summary>
    public static void ApplyRibbonTypography(RibbonControl? ribbon, RibbonStatusBar? statusBar = null)
    {
        if (ribbon != null)
        {
            ribbon.Font = RibbonFont;
            ApplyRibbonControllerTypography(ribbon.Controller ?? BarAndDockingController.Default);
        }

        if (statusBar != null)
        {
            statusBar.Font = StatusFont;
        }
    }

    /// <summary>
    /// Applies standardized CBOS grid typography across rows, header panels, footers, and filters.
    /// </summary>
    public static void ApplyGridTypography(GridView? gridView)
    {
        if (gridView == null) return;

        gridView.Appearance.Row.Font = GridFont;
        gridView.Appearance.Row.Options.UseFont = true;

        gridView.Appearance.HeaderPanel.Font = GridHeaderFont;
        gridView.Appearance.HeaderPanel.Options.UseFont = true;

        gridView.Appearance.GroupRow.Font = GridFont;
        gridView.Appearance.GroupRow.Options.UseFont = true;

        gridView.Appearance.FooterPanel.Font = GridFont;
        gridView.Appearance.FooterPanel.Options.UseFont = true;

        gridView.Appearance.GroupFooter.Font = GridFont;
        gridView.Appearance.GroupFooter.Options.UseFont = true;

        gridView.Appearance.FilterPanel.Font = GridFont;
        gridView.Appearance.FilterPanel.Options.UseFont = true;

        gridView.Appearance.Empty.Font = GridFont;
        gridView.Appearance.Empty.Options.UseFont = true;
    }
}
