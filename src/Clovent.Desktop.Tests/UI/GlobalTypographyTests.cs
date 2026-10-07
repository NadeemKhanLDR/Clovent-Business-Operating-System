using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Dashboard;
using Clovent.Desktop.MasterData;
using DevExpress.Utils;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using Xunit;

namespace Clovent.Desktop.Tests.UI;

public class GlobalTypographyTests
{
    public GlobalTypographyTests()
    {
        // Ensure global typography is initialized for test execution
        DesktopStyle.ApplyGlobalTypography();
    }

    [Fact]
    public void DesktopStyle_SemanticFonts_MeetHighDpiRequirements()
    {
        // Verify font families are Segoe UI
        Assert.Equal("Segoe UI", DesktopStyle.PageHeaderFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.SectionHeaderFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.CardValueFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.BodyFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.BodyFontBold.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.ButtonFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.ButtonFontBold.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.NavigationFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.RibbonFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.RibbonSmallFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.GridFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.GridHeaderFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.CaptionFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.SmallCaptionFont.FontFamily.Name);
        Assert.Equal("Segoe UI", DesktopStyle.StatusFont.FontFamily.Name);

        // Verify logical sizes in points
        Assert.True(DesktopStyle.PageHeaderFont.SizeInPoints >= 16F, "PageHeaderFont should be at least 16pt");
        Assert.True(DesktopStyle.SectionHeaderFont.SizeInPoints >= 11F, "SectionHeaderFont should be at least 11pt");
        Assert.True(DesktopStyle.CardValueFont.SizeInPoints >= 18F, "CardValueFont should be at least 18pt");
        Assert.True(DesktopStyle.BodyFont.SizeInPoints >= 10F, "BodyFont should be at least 10pt");
        Assert.True(DesktopStyle.ButtonFont.SizeInPoints >= 10F, "ButtonFont should be at least 10pt");
        Assert.True(DesktopStyle.NavigationFont.SizeInPoints >= 9.5F, "NavigationFont should be at least 9.5pt");
        Assert.True(DesktopStyle.RibbonFont.SizeInPoints >= 9.5F, "RibbonFont should be at least 9.5pt");
        Assert.True(DesktopStyle.RibbonSmallFont.SizeInPoints >= 9F, "RibbonSmallFont should be at least 9pt");
        Assert.True(DesktopStyle.GridFont.SizeInPoints >= 9.5F, "GridFont should be at least 9.5pt");
        Assert.True(DesktopStyle.GridHeaderFont.SizeInPoints >= 10F, "GridHeaderFont should be at least 10pt");
        Assert.True(DesktopStyle.CaptionFont.SizeInPoints >= 9F, "CaptionFont should be at least 9pt");
        Assert.True(DesktopStyle.StatusFont.SizeInPoints >= 9.5F, "StatusFont should be at least 9.5pt");

        // Verify font weights
        Assert.True(DesktopStyle.PageHeaderFont.Bold);
        Assert.True(DesktopStyle.SectionHeaderFont.Bold);
        Assert.True(DesktopStyle.CardValueFont.Bold);
        Assert.True(DesktopStyle.BodyFontBold.Bold);
        Assert.True(DesktopStyle.ButtonFontBold.Bold);
        Assert.True(DesktopStyle.GridHeaderFont.Bold);
    }

    [Fact]
    public void DesktopStyle_ApplyGlobalTypography_SetsExpectedDefaults()
    {
        DesktopStyle.ApplyGlobalTypography();

        Assert.Equal("Segoe UI", WindowsFormsSettings.DefaultFont.Name);
        Assert.True(WindowsFormsSettings.DefaultFont.SizeInPoints >= 10F);

        Assert.Equal("Segoe UI", WindowsFormsSettings.DefaultMenuFont.Name);
        Assert.True(WindowsFormsSettings.DefaultMenuFont.SizeInPoints >= 9.5F);

        Assert.Equal("Segoe UI", AppearanceObject.DefaultFont.Name);
        Assert.True(AppearanceObject.DefaultFont.SizeInPoints >= 10F);
    }

    [Fact]
    public void DevExpressControls_InheritGlobalDefaultFont_WithoutExplicitStyle()
    {
        DesktopStyle.ApplyGlobalTypography();

        using var button = new SimpleButton();
        Assert.Equal("Segoe UI", button.Font.Name);
        Assert.True(button.Font.SizeInPoints >= 10F);

        using var label = new LabelControl();
        Assert.Equal("Segoe UI", label.Font.Name);
        Assert.True(label.Font.SizeInPoints >= 10F);

        using var listBox = new ListBoxControl();
        Assert.Equal("Segoe UI", listBox.Font.Name);
        Assert.True(listBox.Font.SizeInPoints >= 10F);

        using var textEdit = new TextEdit();
        Assert.Equal("Segoe UI", textEdit.Font.Name);
        Assert.True(textEdit.Font.SizeInPoints >= 10F);

        using var grid = new GridControl();
        using var gridView = new GridView(grid);
        Assert.Equal("Segoe UI", gridView.Appearance.Row.Font.Name);
        Assert.True(gridView.Appearance.Row.Font.SizeInPoints >= 9.5F);
    }

    [Fact]
    public void RibbonTypography_IsProperlyConfiguredOnControllerAndControl()
    {
        DesktopStyle.ApplyGlobalTypography();

        var controller = BarAndDockingController.Default;
        Assert.Equal("Segoe UI", controller.AppearancesRibbon.PageHeader.Font.Name);
        Assert.True(controller.AppearancesRibbon.PageHeader.Font.SizeInPoints >= 9.5F);
        Assert.True(controller.AppearancesRibbon.PageHeader.Options.UseFont);

        Assert.Equal("Segoe UI", controller.AppearancesRibbon.Item.Font.Name);
        Assert.True(controller.AppearancesRibbon.Item.Font.SizeInPoints >= 9.5F);
        Assert.True(controller.AppearancesRibbon.Item.Options.UseFont);

        Assert.Equal("Segoe UI", controller.AppearancesRibbon.PageGroupCaption.Font.Name);
        Assert.True(controller.AppearancesRibbon.PageGroupCaption.Font.SizeInPoints >= 9F);
        Assert.True(controller.AppearancesRibbon.PageGroupCaption.Options.UseFont);

        Assert.Equal("Segoe UI", controller.AppearancesBar.StatusBar.Font.Name);
        Assert.True(controller.AppearancesBar.StatusBar.Font.SizeInPoints >= 9.5F);

        using var ribbon = new RibbonControl();
        using var statusBar = new RibbonStatusBar(ribbon);
        DesktopStyle.ApplyRibbonTypography(ribbon, statusBar);

        Assert.Equal("Segoe UI", ribbon.Font.Name);
        Assert.True(ribbon.Font.SizeInPoints >= 9.5F);
        Assert.Equal("Segoe UI", statusBar.Font.Name);
        Assert.True(statusBar.Font.SizeInPoints >= 9.5F);
    }

    [Fact]
    public void GridTypography_IsProperlyConfiguredWithApplyGridTypography()
    {
        using var gridView = new GridView();
        DesktopStyle.ApplyGridTypography(gridView);

        Assert.Equal(DesktopStyle.GridFont, gridView.Appearance.Row.Font);
        Assert.True(gridView.Appearance.Row.Options.UseFont);

        Assert.Equal(DesktopStyle.GridHeaderFont, gridView.Appearance.HeaderPanel.Font);
        Assert.True(gridView.Appearance.HeaderPanel.Options.UseFont);

        Assert.Equal(DesktopStyle.GridFont, gridView.Appearance.GroupRow.Font);
        Assert.True(gridView.Appearance.GroupRow.Options.UseFont);

        Assert.Equal(DesktopStyle.GridFont, gridView.Appearance.FooterPanel.Font);
        Assert.True(gridView.Appearance.FooterPanel.Options.UseFont);

        Assert.Equal(DesktopStyle.GridFont, gridView.Appearance.FilterPanel.Font);
        Assert.True(gridView.Appearance.FilterPanel.Options.UseFont);
    }

    [Fact]
    public void DashboardView_Typography_AdheresToSemanticStyles()
    {
        using var dashboard = new DashboardView();

        // Title
        var lblTitle = GetField<LabelControl>(dashboard, "lblDashboardTitle");
        Assert.NotNull(lblTitle);
        Assert.Equal("Segoe UI", lblTitle.Appearance.Font.Name);
        Assert.True(lblTitle.Appearance.Font.SizeInPoints >= 16F);

        // Context Captions
        var lblOrgCaption = GetField<LabelControl>(dashboard, "lblOrganizationCaption");
        Assert.NotNull(lblOrgCaption);
        Assert.Equal("Segoe UI", lblOrgCaption.Appearance.Font.Name);
        Assert.True(lblOrgCaption.Appearance.Font.SizeInPoints >= 9F);

        // KPI Captions
        var lblActiveSessionsCaption = GetField<LabelControl>(dashboard, "lblActiveSessionsCaption");
        Assert.NotNull(lblActiveSessionsCaption);
        Assert.Equal("Segoe UI", lblActiveSessionsCaption.Appearance.Font.Name);
        Assert.True(lblActiveSessionsCaption.Appearance.Font.SizeInPoints >= 9F);

        var lblReceivablesCaption = GetField<LabelControl>(dashboard, "lblTotalReceivablesCaption");
        Assert.NotNull(lblReceivablesCaption);
        Assert.Equal("Segoe UI", lblReceivablesCaption.Appearance.Font.Name);
        Assert.True(lblReceivablesCaption.Appearance.Font.SizeInPoints >= 9F);

        // List controls
        var lstActivity = GetField<ListBoxControl>(dashboard, "lstRecentActivity");
        Assert.NotNull(lstActivity);
        Assert.Equal("Segoe UI", lstActivity.Appearance.Font.Name);
        Assert.True(lstActivity.Appearance.Font.SizeInPoints >= 10F);

        var lstNotifications = GetField<ListBoxControl>(dashboard, "lstNotifications");
        Assert.NotNull(lstNotifications);
        Assert.Equal("Segoe UI", lstNotifications.Appearance.Font.Name);
        Assert.True(lstNotifications.Appearance.Font.SizeInPoints >= 10F);

        // Bottom toolbar buttons
        var btnRefresh = GetField<SimpleButton>(dashboard, "btnRefresh");
        Assert.NotNull(btnRefresh);
        Assert.Equal("Segoe UI", btnRefresh.Appearance.Font.Name);
        Assert.True(btnRefresh.Appearance.Font.SizeInPoints >= 10F);

        var btnViewAll = GetField<SimpleButton>(dashboard, "btnViewNotifications");
        Assert.NotNull(btnViewAll);
        Assert.Equal("Segoe UI", btnViewAll.Appearance.Font.Name);
        Assert.True(btnViewAll.Appearance.Font.SizeInPoints >= 10F);
    }

    [Fact]
    public void MasterDataListView_AppliesCentralGridTypography()
    {
        var columns = new MasterDataColumn[]
        {
            new("Code", "Code", 100),
            new("Name", "Name", 200)
        };

        using var listView = new MasterDataListView<object>(columns);

        Assert.Equal(DesktopStyle.GridFont, listView.GridView.Appearance.Row.Font);
        Assert.True(listView.GridView.Appearance.Row.Options.UseFont);

        Assert.Equal(DesktopStyle.GridHeaderFont, listView.GridView.Appearance.HeaderPanel.Font);
        Assert.True(listView.GridView.Appearance.HeaderPanel.Options.UseFont);
    }

    private static T? GetField<T>(object instance, string fieldName) where T : class
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return field?.GetValue(instance) as T;
    }
}
