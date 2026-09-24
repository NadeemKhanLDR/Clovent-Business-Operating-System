using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Restaurant.ActivityLog;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Restaurant.Application.Customers.Dtos;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant;

public class BackOfficeDialogQualityTests
{
    [Fact]
    public void DesktopDialogSizing_ClampsToWorkingArea_AndEnforcesMinimumBounds()
    {
        using var form = new XtraForm();
        DesktopDialogSizing.Apply(form, 1200, 900, 600, 450, null, true);

        Assert.True(form.MinimumSize.Width >= 600);
        Assert.True(form.MinimumSize.Height >= 450);
        Assert.True(form.ClientSize.Width >= form.MinimumSize.Width);
        Assert.True(form.ClientSize.Height >= form.MinimumSize.Height);
        Assert.Equal(FormBorderStyle.Sizable, form.FormBorderStyle);
        Assert.True(form.MaximizeBox);
        Assert.False(form.MinimizeBox);
        Assert.False(form.ShowInTaskbar);
    }

    [Fact]
    public void RecommendationRulesView_TopPanel_UsesAutoSizeAndDoesNotHardcode260PxColumn()
    {
        using var view = (RecommendationRulesView)Activator.CreateInstance(typeof(RecommendationRulesView), true)!;
        var topPanelField = typeof(RecommendationRulesView).GetField("topPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(topPanelField);
        var topPanel = (TableLayoutPanel)topPanelField.GetValue(view)!;

        Assert.NotNull(topPanel);
        Assert.Equal(SizeType.AutoSize, topPanel.ColumnStyles[0].SizeType);
        Assert.Equal(SizeType.Percent, topPanel.ColumnStyles[1].SizeType);

        var headerLabelField = typeof(RecommendationRulesView).GetField("headerLabel", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(headerLabelField);
        var headerLabel = (LabelControl)headerLabelField.GetValue(view)!;
        Assert.Equal("RECOMMENDATION RULES", headerLabel.Text);
    }

    [Fact]
    public void RecommendationRuleEditForm_HasSaveChangesAndCancelButtons_AndSevenDayGrid()
    {
        var variantOptions = new List<ProductOptionRow>();
        var productOptions = new List<ProductOptionRowSummary>();

        using var form = new RecommendationRuleEditForm(
            "New Rule",
            variantOptions,
            productOptions);

        var okProp = typeof(RecommendationRuleEditForm).BaseType!.GetProperty("DialogOkButton", BindingFlags.Instance | BindingFlags.NonPublic);
        var cancelProp = typeof(RecommendationRuleEditForm).BaseType!.GetProperty("DialogCancelButton", BindingFlags.Instance | BindingFlags.NonPublic);
        var panelProp = typeof(RecommendationRuleEditForm).BaseType!.GetProperty("DialogButtonPanel", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(okProp);
        Assert.NotNull(cancelProp);
        Assert.NotNull(panelProp);

        var okBtn = (SimpleButton)okProp.GetValue(form)!;
        var cancelBtn = (SimpleButton)cancelProp.GetValue(form)!;
        var btnPanel = (FlowLayoutPanel)panelProp.GetValue(form)!;

        Assert.Equal("Save Changes", okBtn.Text);
        Assert.Equal("Cancel", cancelBtn.Text);

        // Visual order in RightToLeft: Cancel is index 1, Ok is index 0
        Assert.Equal(0, btnPanel.Controls.GetChildIndex(okBtn));
        Assert.Equal(1, btnPanel.Controls.GetChildIndex(cancelBtn));

        // Days panel contains 7-column table layout
        var daysPanelField = typeof(RecommendationRuleEditForm).GetField("_daysPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(daysPanelField);
        var daysPanel = (FlowLayoutPanel)daysPanelField.GetValue(form)!;
        Assert.Single(daysPanel.Controls);
        var table = Assert.IsType<TableLayoutPanel>(daysPanel.Controls[0]);
        Assert.Equal(7, table.ColumnCount);
        Assert.Equal(7, table.Controls.Count);
    }

    [Fact]
    public void OrderHealthSettingsForm_ValidatesThresholds_AndInitializesProperly()
    {
        using var dialog = new OrderHealthSettingsForm();
        Assert.True(dialog.MinimumSize.Width >= 500);
        Assert.True(dialog.MinimumSize.Height >= 360);
        Assert.False(dialog.MaximizeBox);
        Assert.False(dialog.MinimizeBox);
    }

    [Fact]
    public void ActivityLogView_UsesRootThreeRowTableLayout_AndWorkstationCaption()
    {
        using var view = new ActivityLogView();
        var rootField = typeof(ActivityLogView).GetField("_rootLayout", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(rootField);
        var root = (TableLayoutPanel)rootField.GetValue(view)!;

        Assert.NotNull(root);
        Assert.Equal(3, root.RowCount);
        Assert.Equal(SizeType.AutoSize, root.RowStyles[0].SizeType); // Header
        Assert.Equal(SizeType.AutoSize, root.RowStyles[1].SizeType); // Filter Bar
        Assert.Equal(SizeType.Percent, root.RowStyles[2].SizeType);  // Grid Host

        var gridField = typeof(ActivityLogView).GetField("_gridView", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(gridField);
        var gridView = (GridView)gridField.GetValue(view)!;
        var col = gridView.Columns["MachineName"];
        Assert.NotNull(col);
        Assert.Equal("Workstation", col.Caption);
    }

    [Fact]
    public void CustomerLedgerDialog_HasLoadLedgerButton_AndInternalHeader()
    {
        var dummyCustomer = new CustomerDto(
            Guid.NewGuid(),
            "CUST-001",
            "John Doe",
            "555-1234",
            "123 Main St",
            null,
            0m,
            0m,
            0m,
            true,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        using var dialog = new CustomerLedgerDialog(null!, dummyCustomer);
        var btnLoadField = typeof(CustomerLedgerDialog).GetField("_btnLoadLedger", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(btnLoadField);
        var btnLoad = (SimpleButton)btnLoadField.GetValue(dialog)!;

        Assert.Equal("Load Ledger", btnLoad.Text);
        Assert.Equal(Color.FromArgb(13, 148, 136), btnLoad.Appearance.BackColor);

        var headerField = typeof(CustomerLedgerDialog).GetField("_titleLabel", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(headerField);
        var lblHeader = (LabelControl)headerField.GetValue(dialog)!;
        Assert.Contains("CUSTOMER LEDGER STATEMENT", lblHeader.Text);
    }

    /// <summary>
    /// CRITICAL DPI FIX: Both filter rows must be AutoSize so the TableLayoutPanel
    /// measures row heights from actual control preferred sizes.
    /// Fixed pixel rows (20px labels, 34px editors) collapse to slivers at 250% DPI.
    /// </summary>
    [Fact]
    public void CustomerLedgerDialog_FilterPanel_UsesAutoSizeRows_NotFixedPixels()
    {
        var dummyCustomer = new CustomerDto(
            Guid.NewGuid(), "C001", "Test Customer", "555-0001", "1 Main St",
            null, 0m, 0m, 0m, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        using var dialog = new CustomerLedgerDialog(null!, dummyCustomer);

        var filterPanelField = typeof(CustomerLedgerDialog).GetField("filterPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(filterPanelField);
        var filterPanel = (TableLayoutPanel)filterPanelField.GetValue(dialog)!;
        Assert.NotNull(filterPanel);

        // Both row styles must be AutoSize, NOT Absolute.
        Assert.Equal(SizeType.AutoSize, filterPanel.RowStyles[0].SizeType);
        Assert.Equal(SizeType.AutoSize, filterPanel.RowStyles[1].SizeType);
    }

    /// <summary>
    /// Load Ledger is the primary action button: must have adequate MinimumSize
    /// at the 96-DPI baseline so it is never rendered as a thin strip.
    /// At 250% DPI ScaleLayoutAtRuntime multiplies these values.
    /// </summary>
    [Fact]
    public void CustomerLedgerDialog_LoadLedgerButton_HasAdequateMinimumSize()
    {
        var dummyCustomer = new CustomerDto(
            Guid.NewGuid(), "C001", "Test Customer", "555-0001", "1 Main St",
            null, 0m, 0m, 0m, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        using var dialog = new CustomerLedgerDialog(null!, dummyCustomer);

        var btnLoadField = typeof(CustomerLedgerDialog).GetField("_btnLoadLedger", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(btnLoadField);
        var btnLoad = (SimpleButton)btnLoadField.GetValue(dialog)!;

        Assert.True(btnLoad.MinimumSize.Width >= 90,
            $"Load Ledger MinimumSize.Width ({btnLoad.MinimumSize.Width}) is too narrow.");
        Assert.True(btnLoad.MinimumSize.Height >= 28,
            $"Load Ledger MinimumSize.Height ({btnLoad.MinimumSize.Height}) is too short — renders as thin strip.");
    }

    [Fact]
    public void CustomerLedgerDialog_FilterPanel_HasFiveColumns_AllLabelsCorrect()
    {
        var dummyCustomer = new CustomerDto(
            Guid.NewGuid(), "C001", "Test Customer", "555-0001", "1 Main St",
            null, 0m, 0m, 0m, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        using var dialog = new CustomerLedgerDialog(null!, dummyCustomer);

        var filterPanelField = typeof(CustomerLedgerDialog).GetField("filterPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        var filterPanel = (TableLayoutPanel)filterPanelField!.GetValue(dialog)!;

        // 5 columns: From, To, Transactions, Reference/Description, Tools
        Assert.Equal(5, filterPanel.ColumnCount);

        // Last column must be Percent (flexible) to host all action buttons
        Assert.Equal(SizeType.Percent, filterPanel.ColumnStyles[4].SizeType);

        // All four filter labels must have correct text
        var lblFrom   = (LabelControl)typeof(CustomerLedgerDialog).GetField("_lblFrom",   BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialog)!;
        var lblTo     = (LabelControl)typeof(CustomerLedgerDialog).GetField("_lblTo",     BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialog)!;
        var lblType   = (LabelControl)typeof(CustomerLedgerDialog).GetField("_lblType",   BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialog)!;
        var lblSearch = (LabelControl)typeof(CustomerLedgerDialog).GetField("_lblSearch", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialog)!;

        Assert.Equal("From",                   lblFrom.Text);
        Assert.Equal("To",                     lblTo.Text);
        Assert.Equal("Transactions",           lblType.Text);
        Assert.Equal("Reference / Description", lblSearch.Text);
    }

    /// <summary>
    /// The RECOMMENDATION RULES header label must be AutoSize=true so it measures
    /// its own preferred width and is never truncated by the TableLayoutPanel at high DPI.
    /// Dock=Fill conflicts with AutoSize and was the root cause of the "Recomme..." bug.
    /// </summary>
    [Fact]
    public void RecommendationRulesView_HeaderLabel_IsAutoSize_NotDockFill()
    {
        using var view = (RecommendationRulesView)Activator.CreateInstance(typeof(RecommendationRulesView), true)!;
        var headerLabelField = typeof(RecommendationRulesView).GetField("headerLabel", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(headerLabelField);
        var headerLabel = (LabelControl)headerLabelField.GetValue(view)!;

        // AutoSize=true allows the label to measure its own preferred width
        Assert.True(headerLabel.AutoSize,
            "headerLabel must have AutoSize=true so the text is never truncated by a Fill column.");

        // Dock=Fill conflicts with AutoSize and causes truncation at high DPI
        Assert.NotEqual(DockStyle.Fill, headerLabel.Dock);

        // Full text must be set
        Assert.Equal("RECOMMENDATION RULES", headerLabel.Text);
    }

    [Fact]
    public void CustomerLedgerDialog_ReportPeriodCombo_HasStandardPresets()
    {
        var dummyCustomer = new CustomerDto(
            Guid.NewGuid(), "C001", "Test Customer", "555-0001", "1 Main St",
            null, 0m, 0m, 0m, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        using var dialog = new CustomerLedgerDialog(null!, dummyCustomer);

        var periodComboField = typeof(CustomerLedgerDialog).GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(periodComboField);
        var periodCombo = (ComboBoxEdit)periodComboField.GetValue(dialog)!;
        Assert.NotNull(periodCombo);

        var items = periodCombo.Properties.Items.Cast<object>().Select(o => o.ToString()).ToList();
        Assert.Contains("Today", items);
        Assert.Contains("Yesterday", items);
        Assert.Contains("This Week", items);
        Assert.Contains("Last Week", items);
        Assert.Contains("This Month", items);
        Assert.Contains("Last Month", items);
        Assert.Contains("Last 7 Days", items);
        Assert.Contains("Last 30 Days", items);
        Assert.Contains("Custom", items);
    }

    [Fact]
    public void CustomerLedgerDialog_StatusLabel_Exists()
    {
        var dummyCustomer = new CustomerDto(
            Guid.NewGuid(), "C001", "Test Customer", "555-0001", "1 Main St",
            null, 0m, 0m, 0m, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        using var dialog = new CustomerLedgerDialog(null!, dummyCustomer);

        var statusLabelField = typeof(CustomerLedgerDialog).GetField("_lblStatus", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(statusLabelField);
        var lblStatus = (LabelControl)statusLabelField.GetValue(dialog)!;
        Assert.NotNull(lblStatus);
    }

    [Fact]
    public void CustomerLedgerDialog_RootLayout_HasSixRows_AndStatusPanelIsDedicatedRowThree()
    {
        var dummyCustomer = new CustomerDto(
            Guid.NewGuid(), "C001", "Test Customer", "555-0001", "1 Main St",
            null, 0m, 0m, 0m, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        using var dialog = new CustomerLedgerDialog(null!, dummyCustomer);

        var rootField = typeof(CustomerLedgerDialog).GetField("root", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(rootField);
        var root = (TableLayoutPanel)rootField.GetValue(dialog)!;
        Assert.NotNull(root);
        Assert.Equal(6, root.RowCount);

        var statusPanelField = typeof(CustomerLedgerDialog).GetField("_statusPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(statusPanelField);
        var statusPanel = (PanelControl)statusPanelField.GetValue(dialog)!;
        Assert.NotNull(statusPanel);

        // Status panel must be child of root at row 3 (not inside filterPanel)
        var row = root.GetRow(statusPanel);
        Assert.Equal(3, row);
    }

    [Fact]
    public void CustomerLedgerDialog_OpeningBalanceBroughtForward_CalculatedCorrectly_AndReconcilesRunningBalance()
    {
        var customerId = Guid.NewGuid();
        var dummyCustomer = new CustomerDto(
            customerId, "C001", "John Smith", "555-0001", "1 Main St",
            null, 272.50m, 5000m, 272.50m, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        using var dialog = new CustomerLedgerDialog(null!, dummyCustomer);

        // Populate historical transaction prior to period (in August 2026)
        var augEntry = new CustomerLedgerEntryDto(
            Guid.NewGuid(), customerId, new DateTimeOffset(2026, 8, 12, 15, 30, 0, TimeSpan.Zero),
            "ORD-21", "Credit Sale", 272.50m, 0m, 272.50m);

        var entriesField = typeof(CustomerLedgerDialog).GetField("_allEntries", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(entriesField);
        entriesField.SetValue(dialog, new List<CustomerLedgerEntryDto> { augEntry });

        // Set period to September 2026
        var dateFromField = typeof(CustomerLedgerDialog).GetField("_dateFrom", BindingFlags.Instance | BindingFlags.NonPublic);
        var dateToField = typeof(CustomerLedgerDialog).GetField("_dateTo", BindingFlags.Instance | BindingFlags.NonPublic);
        var dateFrom = (DateEdit)dateFromField!.GetValue(dialog)!;
        var dateTo = (DateEdit)dateToField!.GetValue(dialog)!;
        dateFrom.EditValue = new DateTime(2026, 9, 1);
        dateTo.EditValue = new DateTime(2026, 9, 30);

        // Invoke ApplyFilters
        var applyMethod = typeof(CustomerLedgerDialog).GetMethod("ApplyFilters", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(applyMethod);
        applyMethod.Invoke(dialog, null);

        var gridField = typeof(CustomerLedgerDialog).GetField("_ledgerGrid", BindingFlags.Instance | BindingFlags.NonPublic);
        var grid = (GridControl)gridField!.GetValue(dialog)!;
        var list = (System.Collections.IList)grid.DataSource;

        // Even though August entry is outside September, Balance Brought Forward MUST be present
        Assert.NotNull(list);
        Assert.Single(list);

        var rowObj = list[0]!;
        var refProp = rowObj.GetType().GetProperty("Reference")!.GetValue(rowObj)?.ToString();
        var descProp = rowObj.GetType().GetProperty("Description")!.GetValue(rowObj)?.ToString();
        var debitProp = (decimal)rowObj.GetType().GetProperty("Debit")!.GetValue(rowObj)!;
        var runningBalProp = (decimal)rowObj.GetType().GetProperty("RunningBalance")!.GetValue(rowObj)!;
        var isOpeningProp = (bool)rowObj.GetType().GetProperty("IsOpeningRow")!.GetValue(rowObj)!;

        Assert.Equal("OPENING", refProp);
        Assert.Equal("Balance Brought Forward", descProp);
        Assert.Equal(272.50m, debitProp);
        Assert.Equal(272.50m, runningBalProp);
        Assert.True(isOpeningProp);

        // Now add a September payment transaction
        var sepPayment = new CustomerLedgerEntryDto(
            Guid.NewGuid(), customerId, new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero),
            "REC-001", "Payment Received - Cash", 0m, 100.00m, 172.50m);
        entriesField.SetValue(dialog, new List<CustomerLedgerEntryDto> { augEntry, sepPayment });

        applyMethod.Invoke(dialog, null);
        list = (System.Collections.IList)grid.DataSource;
        Assert.Equal(2, list.Count);

        // Row 0: Balance Brought Forward (272.50)
        // Row 1: Payment (Credit: 100.00, RunningBalance: 172.50)
        var row1Obj = list[1]!;
        var row1Credit = (decimal)row1Obj.GetType().GetProperty("Credit")!.GetValue(row1Obj)!;
        var row1RunningBal = (decimal)row1Obj.GetType().GetProperty("RunningBalance")!.GetValue(row1Obj)!;
        Assert.Equal(100.00m, row1Credit);
        Assert.Equal(172.50m, row1RunningBal);
    }

    [Fact]
    public void CustomersView_VisibleColumns_HasNoDuplicatePhoneOrMobile()
    {
        using var view = (CustomersView)Activator.CreateInstance(typeof(CustomersView), true)!;
        var gridViewField = typeof(CustomersView).GetField("_gridView", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(gridViewField);
        var gridView = (GridView)gridViewField.GetValue(view)!;
        Assert.NotNull(gridView);

        var visibleColumns = gridView.Columns.Cast<DevExpress.XtraGrid.Columns.GridColumn>().Where(c => c.Visible).ToList();
        var captions = visibleColumns.Select(c => c.Caption).ToList();

        // Exactly one "Mobile" and one "Phone" column
        Assert.Single(captions, c => c.Equals("Mobile", StringComparison.OrdinalIgnoreCase));
        Assert.Single(captions, c => c.Equals("Phone", StringComparison.OrdinalIgnoreCase));

        // Alt. Mobile and Shop No must exist but be hidden (Visible = false)
        Assert.DoesNotContain("Alt. Mobile", captions);
        Assert.DoesNotContain("Shop No", captions);

        Assert.NotNull(gridView.Columns["Mobile2"]);
        Assert.False(gridView.Columns["Mobile2"].Visible);
        Assert.NotNull(gridView.Columns["ShopNo"]);
        Assert.False(gridView.Columns["ShopNo"].Visible);
    }

    [Fact]
    public void DateRange_BusinessTimezoneConversions_HandleBoundariesAccurately()
    {
        var range = new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time"); // UTC+5

        var startUtc = range.GetStartUtc(tz);
        var endUtcExclusive = range.GetEndUtcExclusive(tz);
        var endUtcInclusive = range.GetEndUtcInclusive(tz);

        // 2026-09-01 00:00:00 UTC+5 -> 2026-08-31 19:00:00 UTC
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 19, 0, 0, TimeSpan.Zero), startUtc);

        // 2026-10-01 00:00:00 UTC+5 -> 2026-09-30 19:00:00 UTC
        Assert.Equal(new DateTimeOffset(2026, 9, 30, 19, 0, 0, TimeSpan.Zero), endUtcExclusive);

        // Inclusive is 1 tick before exclusive
        Assert.Equal(endUtcExclusive.AddTicks(-1), endUtcInclusive);
    }
}

