using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Restaurant.Setup;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Notifications;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Restaurant.Orders;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using Clovent.Desktop.Restaurant.Reports;
using MediatR;
using Xunit;

namespace Clovent.Desktop.Tests.UI;

/// <summary>
/// Regression assertions for the high-DPI WinForms layout and readability fixes:
/// 1. Restaurant Setup View (POS Setup) starting number, active orders, default order mode, and non-overlapping controls.
/// 2. CommandPanelLayout editor anchoring, minimum height, and section heading AutoSizeMode.
/// 3. Notifications form sizing, readability, minimum dimensions, and accessible Close action.
/// 4. OrganizationHierarchySelector & EntityPicker vertical centering, dropdown rows, and DPI-scaled widths.
/// </summary>
public sealed class UiLayoutRegressionTests
{
    private static T GetField<T>(object instance, string name) where T : class
    {
        var type = instance.GetType();
        while (type != null)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) return (T)field.GetValue(instance)!;
            type = type.BaseType;
        }
        throw new Exception($"Field {name} not found on {instance.GetType().Name}");
    }

    [Fact]
    public void NotificationsForm_HasAppropriateInitialAndMinimumSize_AndAccessibleCloseButton()
    {
        var notifications = new List<Notification>
        {
            new("System Update", "System restarted successfully at 10:00 UTC", DateTime.UtcNow)
        };

        using var form = new NotificationsForm(notifications);
        form.CreateControl();

        var list = GetField<ListBoxControl>(form, "_notificationsList");
        var closeButton = GetField<SimpleButton>(form, "_closeButton");

        Assert.NotNull(list);
        Assert.NotNull(closeButton);

        // Minimum dimensions
        Assert.True(form.MinimumSize.Width >= 440, $"Notifications minimum width {form.MinimumSize.Width} should be >= 440");
        Assert.True(form.MinimumSize.Height >= 320, $"Notifications minimum height {form.MinimumSize.Height} should be >= 320");

        // Client size
        Assert.True(form.ClientSize.Width >= 440, $"Notifications width {form.ClientSize.Width} should be >= 440");
        Assert.True(form.ClientSize.Height >= 320, $"Notifications height {form.ClientSize.Height} should be >= 320");

        // Readable list items
        Assert.True(list.ItemHeight >= 34, $"Notifications list item height {list.ItemHeight} should be >= 34");

        // Header clearance and touch target dimensions
        var headerPanel = GetField<PanelControl>(form, "_headerPanel");
        Assert.NotNull(headerPanel);
        Assert.True(headerPanel.Height >= 56, $"Notifications header panel height {headerPanel.Height} should be >= 56");
        Assert.True(closeButton.Width >= 100, $"Notifications close button width {closeButton.Width} should be >= 100");
        Assert.True(closeButton.Height >= 32, $"Notifications close button height {closeButton.Height} should be >= 32");

        // Accessible close action
        Assert.Equal(DialogResult.OK, closeButton.DialogResult);
        Assert.Equal("Close", closeButton.Text);
        Assert.Same(closeButton, form.CancelButton);
    }

    [Fact]
    public void RestaurantSetupView_NumberingAndPosControls_HaveProperHeightsAndCenterAlignment()
    {
        using var view = new RestaurantSetupView();
        view.CreateControl();

        var prefixEdit = GetField<TextEdit>(view, "_prefixEdit");
        var startingNumberEdit = GetField<SpinEdit>(view, "_startingNumberEdit");
        var activeOrdersRadio = GetField<RadioGroup>(view, "_activeOrdersRadioGroup");
        var defaultOrderModeRadio = GetField<RadioGroup>(view, "_defaultOrderModeRadioGroup");
        var defaultPaymentMethod = GetField<ComboBoxEdit>(view, "_defaultPaymentMethodCombo");

        Assert.NotNull(prefixEdit);
        Assert.NotNull(startingNumberEdit);
        Assert.NotNull(activeOrdersRadio);
        Assert.NotNull(defaultOrderModeRadio);
        Assert.NotNull(defaultPaymentMethod);

        // Starting number must be vertically centered and near-aligned for readability
        Assert.Equal(VertAlignment.Center, startingNumberEdit.Properties.Appearance.TextOptions.VAlignment);
        Assert.Equal(HorzAlignment.Near, startingNumberEdit.Properties.Appearance.TextOptions.HAlignment);

        // Radio groups must not be constrained by restrictive maximum size
        Assert.Equal(Size.Empty, activeOrdersRadio.MaximumSize);
        Assert.Equal(Size.Empty, defaultOrderModeRadio.MaximumSize);
        Assert.True(activeOrdersRadio.Height >= 32, $"Active orders height {activeOrdersRadio.Height} must be >= 32");
        Assert.True(defaultOrderModeRadio.Height >= 32, $"Default order mode height {defaultOrderModeRadio.Height} must be >= 32");

        // Payment method combo dropdown styling
        Assert.Equal(VertAlignment.Center, defaultPaymentMethod.Properties.Appearance.TextOptions.VAlignment);
        Assert.True(defaultPaymentMethod.Properties.DropDownRows >= 8);
    }

    [Fact]
    public void CommandPanelLayout_AddEditor_UsesNoneAnchor_AndHasMinimumHeight()
    {
        using var host = new UserControl();
        var content = new Control();
        var commandFlow = CommandPanelLayout.Build(host, content);

        var editor = new ComboBoxEdit();
        CommandPanelLayout.AddEditor(commandFlow, editor);

        Assert.Equal(AnchorStyles.None, editor.Anchor);
        Assert.True(editor.MinimumSize.Height >= 30, $"Editor minimum height {editor.MinimumSize.Height} must be >= 30");

        var heading = CommandPanelLayout.BuildSectionHeading("Actions");
        Assert.Equal(LabelAutoSizeMode.Default, heading.AutoSizeMode);
    }

    private sealed class DummyMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) => Task.FromResult(default(TResponse)!);
        public Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken ct = default) => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken ct = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default) => throw new NotSupportedException();
    }

    [Fact]
    public void OrganizationHierarchySelector_CombosAndLabels_AreVerticallyCentered_AndWidthIsScaled()
    {
        using var selector = new OrganizationHierarchySelector(new DummyMediator(), showCompany: true, showBranch: true);
        selector.CreateControl();

        var orgCombo = GetField<ComboBoxEdit>(selector, "_organizationCombo");
        var compCombo = GetField<ComboBoxEdit>(selector, "_companyCombo");
        var branchCombo = GetField<ComboBoxEdit>(selector, "_branchCombo");

        foreach (var combo in new[] { orgCombo, compCombo, branchCombo })
        {
            Assert.Equal(VertAlignment.Center, combo.Properties.Appearance.TextOptions.VAlignment);
            Assert.True(combo.Properties.DropDownRows >= 8, "Dropdown rows must be >= 8 to prevent clipped popups");
            Assert.True(combo.Width >= 260, $"Combo width {combo.Width} must be >= 260");
        }
    }

    [Fact]
    public void EntityPicker_CombosAndLabels_AreVerticallyCentered_AndWidthIsScaled()
    {
        using var picker = new EntityPicker("Warehouse:", comboWidth: 260);
        picker.CreateControl();

        var combo = GetField<ComboBoxEdit>(picker, "_combo");
        var label = GetField<LabelControl>(picker, "_label");

        Assert.Equal(VertAlignment.Center, combo.Properties.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, label.Appearance.TextOptions.VAlignment);
        Assert.True(combo.Properties.DropDownRows >= 8);
        Assert.True(combo.Width >= 260);
    }

    [Fact]
    public void RunningOrdersView_OrderTypeFilter_IsProperlySizedAndCentered()
    {
        using var view = (RunningOrdersView)Activator.CreateInstance(typeof(RunningOrdersView))!;
        view.Size = new Size(1000, 600);
        view.PerformLayout();

        var combo = GetField<ComboBoxEdit>(view, "_comboOrderTypeFilter");
        Assert.NotNull(combo);

        Assert.Equal(VertAlignment.Center, combo.Properties.Appearance.TextOptions.VAlignment);
        Assert.True(combo.Properties.DropDownRows >= 8, "Dropdown rows must be >= 8 to prevent clipped items");
        Assert.True(combo.Width >= 180, $"Combo width {combo.Width} must be >= 180");
        Assert.True(combo.Height >= 28, $"Combo height {combo.Height} must be >= 28");

        var topPanel = (TableLayoutPanel)combo.Parent!;
        Assert.NotNull(topPanel);
        Assert.Equal(2, topPanel.ColumnCount);
        Assert.Equal(1, topPanel.RowCount);

        var lbl = (LabelControl)topPanel.GetControlFromPosition(0, 0)!;
        Assert.NotNull(lbl);
        Assert.Equal(AnchorStyles.Left, lbl.Anchor);
        Assert.Equal(VertAlignment.Center, lbl.Appearance.TextOptions.VAlignment);
    }

    [Fact]
    public void CustomerReceivablesReportView_LabelsAreVerticallyCenteredWithEditors()
    {
        using var view = (CustomerReceivablesReportView)Activator.CreateInstance(typeof(CustomerReceivablesReportView))!;
        view.Size = new Size(1000, 600);
        view.PerformLayout();

        var row1Panel = GetField<TableLayoutPanel>(view, "_row1Panel");
        Assert.NotNull(row1Panel);

        var asOfLabel = (LabelControl)row1Panel.GetControlFromPosition(0, 0)!;
        var filterLabel = (LabelControl)row1Panel.GetControlFromPosition(2, 0)!;
        var searchLabel = (LabelControl)row1Panel.GetControlFromPosition(4, 0)!;

        foreach (var label in new[] { asOfLabel, filterLabel, searchLabel })
        {
            Assert.Equal(AnchorStyles.Left, label.Anchor);
            Assert.Equal(0, label.Margin.Top);
            Assert.Equal(0, label.Margin.Bottom);
            Assert.Equal(0, label.Padding.Top);
            Assert.Equal(0, label.Padding.Bottom);
            Assert.Equal(VertAlignment.Center, label.Appearance.TextOptions.VAlignment);
        }
    }

    [Fact]
    public void TaxSummaryReportView_LayoutAndHighDpiScaling_AreResponsiveAndCentered()
    {
        using var view = (TaxSummaryReportView)Activator.CreateInstance(typeof(TaxSummaryReportView), true)!;
        view.Size = new Size(1200, 700);
        view.PerformLayout();

        var warehouseCombo = GetField<ComboBoxEdit>(view, "_warehouseCombo");
        var periodCombo = GetField<ComboBoxEdit>(view, "_periodCombo");
        var fromDateEdit = GetField<DateEdit>(view, "_fromDateEdit");
        var toDateEdit = GetField<DateEdit>(view, "_toDateEdit");
        var refreshButton = GetField<SimpleButton>(view, "_refreshButton");

        var warehouseLabel = GetField<LabelControl>(view, "_warehouseLabel");
        var periodLabel = GetField<LabelControl>(view, "_periodLabel");
        var fromLabel = GetField<LabelControl>(view, "_fromLabel");
        var toLabel = GetField<LabelControl>(view, "_toLabel");

        var filterFlow = GetField<FlowLayoutPanel>(view, "_filterFlow");
        var kpiFlow = GetField<FlowLayoutPanel>(view, "_kpiFlow");

        // 1. Minimum dimensions & AutoHeight
        Assert.False(warehouseCombo.Properties.AutoHeight, "Warehouse combo must have AutoHeight = false");
        Assert.False(periodCombo.Properties.AutoHeight, "Period combo must have AutoHeight = false");
        Assert.False(fromDateEdit.Properties.AutoHeight, "From date edit must have AutoHeight = false");
        Assert.False(toDateEdit.Properties.AutoHeight, "To date edit must have AutoHeight = false");

        Assert.True(warehouseCombo.MinimumSize.Width >= 260, $"Warehouse min width {warehouseCombo.MinimumSize.Width} must be >= 260");
        Assert.True(periodCombo.MinimumSize.Width >= 140, $"Period min width {periodCombo.MinimumSize.Width} must be >= 140");
        Assert.True(fromDateEdit.MinimumSize.Width >= 130, $"From date min width {fromDateEdit.MinimumSize.Width} must be >= 130");
        Assert.True(toDateEdit.MinimumSize.Width >= 130, $"To date min width {toDateEdit.MinimumSize.Width} must be >= 130");
        Assert.True(refreshButton.MinimumSize.Width >= 140, $"Refresh button min width {refreshButton.MinimumSize.Width} must be >= 140");

        Assert.True(warehouseCombo.MinimumSize.Height >= 28, $"Warehouse min height {warehouseCombo.MinimumSize.Height} must be >= 28");
        Assert.True(periodCombo.MinimumSize.Height >= 28, $"Period min height {periodCombo.MinimumSize.Height} must be >= 28");
        Assert.True(fromDateEdit.MinimumSize.Height >= 28, $"From date min height {fromDateEdit.MinimumSize.Height} must be >= 28");
        Assert.True(toDateEdit.MinimumSize.Height >= 28, $"To date min height {toDateEdit.MinimumSize.Height} must be >= 28");
        Assert.True(refreshButton.MinimumSize.Height >= 28, $"Refresh button min height {refreshButton.MinimumSize.Height} must be >= 28");

        // 2. Vertical Centering
        Assert.Equal(VertAlignment.Center, warehouseCombo.Properties.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, periodCombo.Properties.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, fromDateEdit.Properties.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, toDateEdit.Properties.Appearance.TextOptions.VAlignment);

        Assert.Equal(VertAlignment.Center, warehouseLabel.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, periodLabel.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, fromLabel.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, toLabel.Appearance.TextOptions.VAlignment);

        Assert.Equal(0, warehouseLabel.Padding.Top);
        Assert.Equal(0, periodLabel.Padding.Top);
        Assert.Equal(0, fromLabel.Padding.Top);
        Assert.Equal(0, toLabel.Padding.Top);

        // 3. Dropdown Rows and Date Formatting
        Assert.True(warehouseCombo.Properties.DropDownRows >= 8);
        Assert.True(periodCombo.Properties.DropDownRows >= 8);
        Assert.Equal("dd-MMM-yyyy", fromDateEdit.Properties.DisplayFormat.FormatString);
        Assert.Equal("dd-MMM-yyyy", toDateEdit.Properties.DisplayFormat.FormatString);

        // 4. Paired TableLayoutPanel structure
        var warehousePair = GetField<TableLayoutPanel>(view, "_warehousePair");
        var periodPair = GetField<TableLayoutPanel>(view, "_periodPair");
        var fromPair = GetField<TableLayoutPanel>(view, "_fromPair");
        var toPair = GetField<TableLayoutPanel>(view, "_toPair");

        Assert.Same(warehousePair, warehouseCombo.Parent);
        Assert.Same(warehousePair, warehouseLabel.Parent);
        Assert.Same(periodPair, periodCombo.Parent);
        Assert.Same(periodPair, periodLabel.Parent);
        Assert.Same(fromPair, fromDateEdit.Parent);
        Assert.Same(fromPair, fromLabel.Parent);
        Assert.Same(toPair, toDateEdit.Parent);
        Assert.Same(toPair, toLabel.Parent);

        // 5. Responsive wrapping containers
        Assert.True(filterFlow.WrapContents, "Filter flow must allow wrapping to additional rows");
        Assert.True(kpiFlow.WrapContents, "KPI flow must allow wrapping to additional rows");

        // 6. Responsive narrow width wrapping test
        view.Size = new Size(500, 600);
        view.PerformLayout();
        filterFlow.PerformLayout();
        kpiFlow.PerformLayout();

        Assert.True(filterFlow.Height > 40, "Filter flow must expand height when wrapping occurs");

        // 7. Refresh button caption intact
        Assert.Equal("Generate Report", refreshButton.Text);
    }

    [Fact]
    public void RefundAuditReportView_LayoutAndHighDpiScaling_AreResponsiveAndCentered()
    {
        using var view = (RefundAuditReportView)Activator.CreateInstance(typeof(RefundAuditReportView), true)!;
        view.Size = new Size(1200, 700);
        view.PerformLayout();

        var warehouseCombo = GetField<ComboBoxEdit>(view, "_warehouseCombo");
        var periodCombo = GetField<ComboBoxEdit>(view, "_periodCombo");
        var fromDateEdit = GetField<DateEdit>(view, "_fromDateEdit");
        var toDateEdit = GetField<DateEdit>(view, "_toDateEdit");
        var refreshButton = GetField<SimpleButton>(view, "_refreshButton");

        var warehouseLabel = GetField<LabelControl>(view, "_warehouseLabel");
        var periodLabel = GetField<LabelControl>(view, "_periodLabel");
        var fromLabel = GetField<LabelControl>(view, "_fromLabel");
        var toLabel = GetField<LabelControl>(view, "_toLabel");

        var filterFlow = GetField<FlowLayoutPanel>(view, "_filterFlow");
        var kpiFlow = GetField<FlowLayoutPanel>(view, "_kpiFlow");

        // 1. Minimum dimensions & AutoHeight
        Assert.False(warehouseCombo.Properties.AutoHeight, "Warehouse combo must have AutoHeight = false");
        Assert.False(periodCombo.Properties.AutoHeight, "Period combo must have AutoHeight = false");
        Assert.False(fromDateEdit.Properties.AutoHeight, "From date edit must have AutoHeight = false");
        Assert.False(toDateEdit.Properties.AutoHeight, "To date edit must have AutoHeight = false");

        Assert.True(warehouseCombo.MinimumSize.Width >= 260, $"Warehouse min width {warehouseCombo.MinimumSize.Width} must be >= 260");
        Assert.True(periodCombo.MinimumSize.Width >= 140, $"Period min width {periodCombo.MinimumSize.Width} must be >= 140");
        Assert.True(fromDateEdit.MinimumSize.Width >= 130, $"From date min width {fromDateEdit.MinimumSize.Width} must be >= 130");
        Assert.True(toDateEdit.MinimumSize.Width >= 130, $"To date min width {toDateEdit.MinimumSize.Width} must be >= 130");
        Assert.True(refreshButton.MinimumSize.Width >= 140, $"Refresh button min width {refreshButton.MinimumSize.Width} must be >= 140");

        Assert.True(warehouseCombo.MinimumSize.Height >= 28, $"Warehouse min height {warehouseCombo.MinimumSize.Height} must be >= 28");
        Assert.True(periodCombo.MinimumSize.Height >= 28, $"Period min height {periodCombo.MinimumSize.Height} must be >= 28");
        Assert.True(fromDateEdit.MinimumSize.Height >= 28, $"From date min height {fromDateEdit.MinimumSize.Height} must be >= 28");
        Assert.True(toDateEdit.MinimumSize.Height >= 28, $"To date min height {toDateEdit.MinimumSize.Height} must be >= 28");
        Assert.True(refreshButton.MinimumSize.Height >= 28, $"Refresh button min height {refreshButton.MinimumSize.Height} must be >= 28");

        // 2. Vertical Centering
        Assert.Equal(VertAlignment.Center, warehouseCombo.Properties.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, periodCombo.Properties.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, fromDateEdit.Properties.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, toDateEdit.Properties.Appearance.TextOptions.VAlignment);

        Assert.Equal(VertAlignment.Center, warehouseLabel.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, periodLabel.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, fromLabel.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, toLabel.Appearance.TextOptions.VAlignment);

        Assert.Equal(0, warehouseLabel.Padding.Top);
        Assert.Equal(0, periodLabel.Padding.Top);
        Assert.Equal(0, fromLabel.Padding.Top);
        Assert.Equal(0, toLabel.Padding.Top);

        // 3. Dropdown Rows and Date Formatting
        Assert.True(warehouseCombo.Properties.DropDownRows >= 8);
        Assert.True(periodCombo.Properties.DropDownRows >= 8);
        Assert.Equal("dd-MMM-yyyy", fromDateEdit.Properties.DisplayFormat.FormatString);
        Assert.Equal("dd-MMM-yyyy", toDateEdit.Properties.DisplayFormat.FormatString);

        // 4. Paired TableLayoutPanel structure
        var warehousePair = GetField<TableLayoutPanel>(view, "_warehousePair");
        var periodPair = GetField<TableLayoutPanel>(view, "_periodPair");
        var fromPair = GetField<TableLayoutPanel>(view, "_fromPair");
        var toPair = GetField<TableLayoutPanel>(view, "_toPair");

        Assert.Same(warehousePair, warehouseCombo.Parent);
        Assert.Same(warehousePair, warehouseLabel.Parent);
        Assert.Same(periodPair, periodCombo.Parent);
        Assert.Same(periodPair, periodLabel.Parent);
        Assert.Same(fromPair, fromDateEdit.Parent);
        Assert.Same(fromPair, fromLabel.Parent);
        Assert.Same(toPair, toDateEdit.Parent);
        Assert.Same(toPair, toLabel.Parent);

        // 5. Responsive wrapping containers
        Assert.True(filterFlow.WrapContents, "Filter flow must allow wrapping to additional rows");
        Assert.True(kpiFlow.WrapContents, "KPI flow must allow wrapping to additional rows");

        // 6. Responsive narrow width wrapping test
        view.Size = new Size(500, 600);
        view.PerformLayout();
        filterFlow.PerformLayout();
        kpiFlow.PerformLayout();

        Assert.True(filterFlow.Height > 40, "Filter flow must expand height when wrapping occurs");

        // 7. Refresh button caption intact
        Assert.Equal("Generate Report", refreshButton.Text);
    }
}
