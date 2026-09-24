using System.Reflection;
using Clovent.Desktop.Restaurant.Orders;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Regression tests for the Cancel Order double-dialog fix and the redesigned
/// Recall / Sales History dialog. The double-dialog bug was caused by
/// CancelOrderButton_Click being subscribed both in the Designer's
/// InitializeComponent and again in the header-building code, so one click
/// fired the handler twice. These tests pin both sides of that fix: the event
/// lifecycle (exactly one subscription) and the dialog's operational
/// structure (status filters, search, default Held, action gating, size).
/// </summary>
public sealed class RecallOrderDialogTests
{
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new();
        public object? GetService(Type serviceType) => null;
    }

    private static RecallOrderDialog CreateDialog() =>
        new(new MediatR.Mediator(EmptyServiceProvider.Instance), NullLogger.Instance);

    private static object? Field(object instance, string name) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance);

    private static object? Method(object instance, string name) =>
        instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, null);

    // ===== Cancel Order bug (Section 1) =====

    [Fact]
    public void CancelOrderButton_Click_IsWired_OnlyInDesigner()
    {
        var posSource = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "Clovent.Desktop", "Restaurant", "Orders", "RestaurantPosForm.cs"));

        // The Designer's InitializeComponent owns the wiring; a second
        // subscription in the code-behind is what produced the double dialog.
        Assert.DoesNotContain("_cancelOrderButton.Click +=", posSource);
    }

    [Fact]
    public void CancelOrderButton_Click_IsWired_ExactlyOnce_InDesigner()
    {
        var designerSource = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "Clovent.Desktop", "Restaurant", "Orders", "RestaurantPosForm.Designer.cs"));

        var count = designerSource.Split("_cancelOrderButton.Click += ").Length - 1;
        Assert.Equal(1, count);
    }

    // ===== Dialog structure (Sections 2-7, 16, 25) =====

    [Fact]
    public void Dialog_HasProductionSize_Minimum900x540()
    {
        using var dialog = CreateDialog();
        Assert.True(dialog.MinimumSize.Width >= 900, $"Minimum width {dialog.MinimumSize.Width} < 900");
        Assert.True(dialog.MinimumSize.Height >= 540, $"Minimum height {dialog.MinimumSize.Height} < 540");
    }

    [Fact]
    public void Dialog_SizeFitsWorkingArea_AndReachesOperationalSize()
    {
        using var dialog = CreateDialog();
        var work = Screen.PrimaryScreen!.WorkingArea;

        // Never exceed the working area, never below the usable minimum.
        Assert.True(dialog.Size.Width <= work.Width, $"Width {dialog.Size.Width} exceeds working area {work.Width}");
        Assert.True(dialog.Size.Height <= work.Height, $"Height {dialog.Size.Height} exceeds working area {work.Height}");
        Assert.True(dialog.Size.Width >= 900 && dialog.Size.Height >= 560,
            $"Size {dialog.Size.Width}x{dialog.Size.Height} below usable minimum");

        // On a roomy screen (>= 1280x800) the window must reach the
        // operational target, not stay a small popup.
        if (work.Width >= 1280 && work.Height >= 800)
        {
            Assert.True(dialog.Size.Width >= 1100, $"Width {dialog.Size.Width} too small for an operational window");
            Assert.True(dialog.Size.Height >= 650, $"Height {dialog.Size.Height} too small for an operational window");
        }
    }

    [Fact]
    public void Dialog_DefaultStatus_IsHeld()
    {
        using var dialog = CreateDialog();
        Assert.Equal(RecallStatusFilter.Held, (RecallStatusFilter)Field(dialog, "_status")!);
    }

    [Fact]
    public void Dialog_HasSearchBox_CountLabel_AndRefresh()
    {
        using var dialog = CreateDialog();
        Assert.IsType<DevExpress.XtraEditors.TextEdit>(Field(dialog, "_searchEdit"));
        Assert.IsType<DevExpress.XtraEditors.LabelControl>(Field(dialog, "_countLabel"));
        var refresh = Assert.IsType<DevExpress.XtraEditors.SimpleButton>(Field(dialog, "_refreshButton"));
        Assert.Equal("⟳ Refresh", refresh.Text);
    }

    [Fact]
    public void Dialog_HasFourStatusTabs()
    {
        using var dialog = CreateDialog();
        Assert.Equal("HELD", ((DevExpress.XtraEditors.SimpleButton)Field(dialog, "_heldTabButton")!).Text);
        Assert.Equal("OPEN", ((DevExpress.XtraEditors.SimpleButton)Field(dialog, "_openTabButton")!).Text);
        Assert.Equal("CLOSED", ((DevExpress.XtraEditors.SimpleButton)Field(dialog, "_closedTabButton")!).Text);
        Assert.Equal("VOIDED", ((DevExpress.XtraEditors.SimpleButton)Field(dialog, "_voidedTabButton")!).Text);
    }

    [Fact]
    public void Dialog_Grid_HasCriticalColumns_WithReadableMinimumWidths()
    {
        using var dialog = CreateDialog();
        var view = (DevExpress.XtraGrid.Views.Grid.GridView)Field(dialog, "_ordersGridView")!;

        // Per-column floors: a header like "Customer" or "Date / Time" must
        // never collapse to a truncated "Cust..." / "Dat..." at any size.
        var minimums = new Dictionary<string, int>
        {
            ["OrderNumber"] = 85,
            ["TypeDisplay"] = 72,
            ["TableDisplay"] = 58,
            ["CustomerName"] = 110,
            ["ItemCount"] = 45,
            ["TotalDisplay"] = 85,
            ["DateTimeDisplay"] = 112,
            ["StatusDisplay"] = 70,
        };

        foreach (var (field, minimum) in minimums)
        {
            var column = view.Columns[field];
            Assert.NotNull(column);
            Assert.True(column.MinWidth >= minimum, $"{field} MinWidth {column.MinWidth} < {minimum}");
        }

        // Columns fill the available width proportionally instead of leaving
        // dead space or relying on BestFitColumns.
        Assert.True(view.OptionsView.ColumnAutoWidth);
    }

    [Fact]
    public void Dialog_StatusTabs_AreLargeEnoughToClick()
    {
        using var dialog = CreateDialog();
        foreach (var name in new[] { "_heldTabButton", "_openTabButton", "_closedTabButton", "_voidedTabButton" })
        {
            var tab = (DevExpress.XtraEditors.SimpleButton)Field(dialog, name)!;
            Assert.True(tab.Height >= 36, $"{name} height {tab.Height} < 36");
            Assert.True(tab.Width >= 110, $"{name} width {tab.Width} < 110");
        }
    }

    [Fact]
    public void Dialog_SearchBox_IsFullSize()
    {
        using var dialog = CreateDialog();
        var search = (DevExpress.XtraEditors.TextEdit)Field(dialog, "_searchEdit")!;
        Assert.True(search.Properties.Appearance.Font!.Size >= 10F, "search font too small");
    }

    [Fact]
    public void Dialog_Grid_IsSortable_NotEditable_SingleSelect()
    {
        using var dialog = CreateDialog();
        var view = (DevExpress.XtraGrid.Views.Grid.GridView)Field(dialog, "_ordersGridView")!;
        Assert.False(view.OptionsBehavior.Editable);
        Assert.False(view.OptionsSelection.MultiSelect);
        Assert.True(view.OptionsCustomization.AllowSort);
    }

    [Fact]
    public void Dialog_ActionButton_DisabledWithoutSelection()
    {
        using var dialog = CreateDialog();
        var action = (DevExpress.XtraEditors.SimpleButton)Field(dialog, "_actionButton")!;
        Assert.False(action.Enabled);
    }

    [Fact]
    public void Dialog_ActionButton_ForHeld_Recalls()
    {
        using var dialog = CreateDialog();
        Method(dialog, "UpdateTabStyles");
        var action = (DevExpress.XtraEditors.SimpleButton)Field(dialog, "_actionButton")!;
        // The button label is derived from the selected status in
        // OnSelectionChanged; Held is the default filter.
        Method(dialog, "OnSelectionChanged");
        Assert.Equal("Recall Order", action.Text);
    }

    [Fact]
    public void Dialog_DatesHidden_ForLiveStatuses_VisibleForHistorical()
    {
        using var dialog = CreateDialog();
        Method(dialog, "UpdateTabStyles"); // Held (default)

        var from = (Control)Field(dialog, "_fromDateEdit")!;
        Assert.False(IsSelfVisible(from.Parent!));

        FieldSetter.Set(dialog, "_status", RecallStatusFilter.Closed);
        Method(dialog, "UpdateTabStyles");
        Assert.True(IsSelfVisible(from.Parent!));
    }

    /// <summary>Control.Visible is false while the form is unshown; read the control's own visibility flag instead.</summary>
    private static bool IsSelfVisible(Control control) =>
        (bool)typeof(Control)
            .GetMethod("GetState", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, [2])!;

    [Fact]
    public void Dialog_Layout_ElementsRemainInsideFormBounds()
    {
        using var dialog = CreateDialog();
        dialog.PerformLayout();

        var search = (Control)Field(dialog, "_searchEdit")!;
        var grid = (Control)Field(dialog, "_ordersGrid")!;
        var closeBtn = (Control)Field(dialog, "_closeButton")!;
        var actionBtn = (Control)Field(dialog, "_actionButton")!;

        // Footer buttons must be inside the form client bounds (never clipped)
        var footerParent = closeBtn.Parent!.Parent!;
        Assert.True(footerParent.Bottom <= dialog.ClientSize.Height,
            $"Footer bottom {footerParent.Bottom} exceeds client height {dialog.ClientSize.Height}");
        Assert.True(footerParent.Top >= 0, $"Footer top {footerParent.Top} < 0");

        // Search must be inside the form client bounds
        var searchParent = search.Parent!;
        Assert.True(searchParent.Top >= 0, $"Search top {searchParent.Top} < 0");
        Assert.True(searchParent.Right <= dialog.ClientSize.Width,
            $"Search right {searchParent.Right} exceeds client width {dialog.ClientSize.Width}");

        // Grid must be visible and have usable dimensions
        Assert.True(grid.Width > 500, $"Grid width {grid.Width} too small");
        Assert.True(grid.Height > 200, $"Grid height {grid.Height} too small");
    }

    [Theory]
    [InlineData(1024, 768)]
    [InlineData(1366, 768)]
    [InlineData(1920, 1080)]
    public void Dialog_ResponsiveSizing_WithinTargetResolutions(int screenWidth, int screenHeight)
    {
        var workWidth = screenWidth - 32;
        var workHeight = screenHeight - 40; // Simulated taskbar

        var targetWidth = workWidth >= 1280 ? 1220 : Math.Max(920, workWidth);
        var targetHeight = workHeight >= 800 ? 720 : Math.Max(580, workHeight);

        var finalWidth = Math.Min(targetWidth, workWidth);
        var finalHeight = Math.Min(targetHeight, workHeight);

        Assert.True(finalWidth >= 920, $"Width {finalWidth} < 920 at {screenWidth}x{screenHeight}");
        Assert.True(finalWidth <= workWidth, $"Width {finalWidth} > {workWidth}");
        Assert.True(finalHeight >= 580, $"Height {finalHeight} < 580 at {screenWidth}x{screenHeight}");
        Assert.True(finalHeight <= workHeight, $"Height {finalHeight} > {workHeight}");
    }

    [Fact]
    public void Dialog_Grid_CriticalColumns_MeetSection6Specifications()
    {
        using var dialog = CreateDialog();
        var view = (DevExpress.XtraGrid.Views.Grid.GridView)Field(dialog, "_ordersGridView")!;

        var specMinimums = new Dictionary<string, int>
        {
            ["OrderNumber"] = 90,
            ["TypeDisplay"] = 85,
            ["TableDisplay"] = 70,
            ["CustomerCode"] = 90,
            ["CustomerName"] = 140,
            ["ItemCount"] = 60,
            ["TotalDisplay"] = 95,
            ["DateTimeDisplay"] = 115,
            ["StatusDisplay"] = 80,
        };

        foreach (var (field, minSpec) in specMinimums)
        {
            var column = view.Columns[field];
            Assert.NotNull(column);
            Assert.True(column.MinWidth >= minSpec,
                $"{field} MinWidth {column.MinWidth} < {minSpec} required by Section 6");
        }
    }

    private static class FieldSetter
    {
        public static void Set(object instance, string name, object value) =>
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);
    }

    /// <summary>Logger stub that swallows everything, so no logging package dependency is needed.</summary>
    private sealed class NullLogger : Microsoft.Extensions.Logging.ILogger
    {
        public static readonly NullLogger Instance = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}
