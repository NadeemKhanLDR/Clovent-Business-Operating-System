using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.DayClose.Commands;
using Clovent.Restaurant.Application.DayClose.Dtos;
using Clovent.Restaurant.Application.DayClose.Queries;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;
using MediatR;

namespace Clovent.Desktop.Restaurant.EndOfDay;

/// <summary>
/// Professional manager dialog for reviewing and executing Business Day Close.
/// Strictly enforces that all shifts on the business date must be closed prior to Day Close.
/// Displays comprehensive financial reconciliation tabs:
/// Sales Summary, Tax Breakdown, Tender Summary, Cash Drawer Reconciliation,
/// Cashier Performance, and Order Type Distribution.
/// Includes Print, PDF Export, and Excel Export backed by canonical summary figures.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class EndOfDayCloseDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly ICurrentSession _currentSession;
    private readonly Guid _branchId;
    private readonly string _branchName;
    private readonly DateOnly _businessDate;

    private BusinessDaySummaryDto? _summary;

    private LabelControl _lblHeader = null!;
    private PanelControl _bannerPanel = null!;
    private LabelControl _lblBanner = null!;

    // Top KPI cards
    private LabelControl _lblGrossSales = null!;
    private LabelControl _lblDiscounts = null!;
    private LabelControl _lblNetSales = null!;
    private LabelControl _lblTax = null!;
    private LabelControl _lblExpectedCash = null!;
    private LabelControl _lblActualCash = null!;
    private LabelControl _lblCashVariance = null!;
    private LabelControl _lblRefunds = null!;

    // Multi-tab container
    private XtraTabControl _tabControl = null!;

    // Tab 1: Shifts Grid
    private GridControl _gridShifts = null!;
    private GridView _viewShifts = null!;

    // Tab 2: Sales Summary Detail Grid
    private GridControl _gridSalesMetrics = null!;
    private GridView _viewSalesMetrics = null!;

    // Tab 3: Tax Breakdown Grid
    private GridControl _gridTaxBreakdown = null!;
    private GridView _viewTaxBreakdown = null!;

    // Tab 4: Tender Breakdown Grid
    private GridControl _gridTenders = null!;
    private GridView _viewTenders = null!;

    // Tab 5: Cash Drawer Reconciliation Grid
    private GridControl _gridCashDrawer = null!;
    private GridView _viewCashDrawer = null!;

    // Tab 6: Cashier Performance Grid
    private GridControl _gridCashiers = null!;
    private GridView _viewCashiers = null!;

    // Tab 7: Order Type Analysis Grid
    private GridControl _gridOrderTypes = null!;
    private GridView _viewOrderTypes = null!;

    private MemoEdit _txtNotes = null!;
    private SimpleButton _btnPrintReport = null!;
    private SimpleButton _btnExportPdf = null!;
    private SimpleButton _btnExportExcel = null!;
    private SimpleButton _btnCloseDay = null!;
    private SimpleButton _btnCancel = null!;

    /// <summary>Result of the completed day close command, if executed.</summary>
    public BusinessDayCloseDto? ClosedResult { get; private set; }

    /// <summary>Design-time constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public EndOfDayCloseDialog()
    {
        _mediator = null!;
        _currentSession = null!;
        _branchId = Guid.Empty;
        _branchName = "Branch";
        _businessDate = DateOnly.FromDateTime(DateTime.Today);
        BuildUi();
    }

    /// <summary>Initializes a new instance of <see cref="EndOfDayCloseDialog"/> for the specified branch and business date.</summary>
    public EndOfDayCloseDialog(
        IMediator mediator,
        ICurrentSession currentSession,
        Guid branchId,
        string branchName,
        DateOnly businessDate)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _currentSession = currentSession ?? throw new ArgumentNullException(nameof(currentSession));
        _branchId = branchId;
        _branchName = branchName;
        _businessDate = businessDate;

        BuildUi();
        Load += async (s, e) => await LoadSummaryAsync();
    }

    private void BuildUi()
    {
        Text = $"End of Day (Day Close & Financial Reconciliation) - {DateTimeDisplay.FormatDate(_businessDate)}";
        AutoScaleMode = AutoScaleMode.None;
        DesktopDialogSizing.Apply(this, 1020, 740, 880, 600, null, false);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(DesktopDpi.Scale(16, this))
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // 0: Header
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));  // 1: Alert/Status Banner
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));  // 2: Metrics Summary Cards
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 3: Tabbed Grids & Reconciliation
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));  // 4: Notes
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));  // 5: Action Buttons

        // 1. Header
        var pnlHeader = new Panel { Dock = DockStyle.Fill };
        _lblHeader = new LabelControl
        {
            Text = $"Business Day Close Summary: {_branchName} ({DateTimeDisplay.FormatDate(_businessDate)})",
            Appearance = { Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) },
            Dock = DockStyle.Top
        };
        var lblSubheader = new LabelControl
        {
            Text = "Authoritative financial reconciliation, statutory tax breakdown, and drawer audit prior to locking business date.",
            Appearance = { Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(100, 116, 139) },
            Dock = DockStyle.Bottom
        };
        pnlHeader.Controls.Add(_lblHeader);
        pnlHeader.Controls.Add(lblSubheader);
        mainPanel.Controls.Add(pnlHeader, 0, 0);

        // 2. Alert Banner
        _bannerPanel = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple
        };
        _lblBanner = new LabelControl
        {
            Dock = DockStyle.Fill,
            Appearance = { Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextOptions = { HAlignment = HorzAlignment.Center, VAlignment = VertAlignment.Center } },
            Text = "Loading business day data..."
        };
        _bannerPanel.Controls.Add(_lblBanner);
        mainPanel.Controls.Add(_bannerPanel, 0, 1);

        // 3. Metrics Summary Cards (2 rows x 4 cols)
        var tlpMetrics = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Margin = new Padding(0, 4, 0, 4)
        };
        tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpMetrics.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        tlpMetrics.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        _lblGrossSales = CreateMetricCard("Gross Sales", "-", Color.FromArgb(30, 41, 59), tlpMetrics, 0, 0);
        _lblDiscounts = CreateMetricCard("Discounts", "-", Color.FromArgb(220, 38, 38), tlpMetrics, 1, 0);
        _lblNetSales = CreateMetricCard("Net Sales", "-", Color.FromArgb(13, 148, 136), tlpMetrics, 2, 0);
        _lblTax = CreateMetricCard("Tax (Sales / VAT)", "-", Color.FromArgb(30, 41, 59), tlpMetrics, 3, 0);

        _lblExpectedCash = CreateMetricCard("Expected Drawer Cash", "-", Color.FromArgb(30, 41, 59), tlpMetrics, 0, 1);
        _lblActualCash = CreateMetricCard("Counted Drawer Cash", "-", Color.FromArgb(30, 41, 59), tlpMetrics, 1, 1);
        _lblCashVariance = CreateMetricCard("Drawer Over / Short", "-", Color.FromArgb(30, 41, 59), tlpMetrics, 2, 1);
        _lblRefunds = CreateMetricCard("Refunds & Reversals", "-", Color.FromArgb(220, 38, 38), tlpMetrics, 3, 1);

        mainPanel.Controls.Add(tlpMetrics, 0, 2);

        // 4. Tabbed Detail Grids
        _tabControl = new XtraTabControl
        {
            Dock = DockStyle.Fill,
            HeaderAutoFill = DefaultBoolean.False
        };
        _tabControl.AppearancePage.Header.Font = new Font("Segoe UI", 9F);

        // Tab A: Shifts
        var pageShifts = new XtraTabPage { Text = "Cashier Shifts" };
        _gridShifts = new GridControl { Dock = DockStyle.Fill };
        _viewShifts = new GridView(_gridShifts);
        SetupStandardGridView(_gridShifts, _viewShifts);
        pageShifts.Controls.Add(_gridShifts);
        _tabControl.TabPages.Add(pageShifts);

        // Tab B: Sales Summary Breakdown
        var pageSales = new XtraTabPage { Text = "Sales Summary" };
        _gridSalesMetrics = new GridControl { Dock = DockStyle.Fill };
        _viewSalesMetrics = new GridView(_gridSalesMetrics);
        SetupStandardGridView(_gridSalesMetrics, _viewSalesMetrics);
        pageSales.Controls.Add(_gridSalesMetrics);
        _tabControl.TabPages.Add(pageSales);

        // Tab C: Tax Breakdown
        var pageTax = new XtraTabPage { Text = "Tax Breakdown" };
        _gridTaxBreakdown = new GridControl { Dock = DockStyle.Fill };
        _viewTaxBreakdown = new GridView(_gridTaxBreakdown);
        SetupStandardGridView(_gridTaxBreakdown, _viewTaxBreakdown);
        pageTax.Controls.Add(_gridTaxBreakdown);
        _tabControl.TabPages.Add(pageTax);

        // Tab D: Tender Summary
        var pageTenders = new XtraTabPage { Text = "Payment Tenders" };
        _gridTenders = new GridControl { Dock = DockStyle.Fill };
        _viewTenders = new GridView(_gridTenders);
        SetupStandardGridView(_gridTenders, _viewTenders);
        pageTenders.Controls.Add(_gridTenders);
        _tabControl.TabPages.Add(pageTenders);

        // Tab E: Cash Drawer Reconciliation
        var pageDrawer = new XtraTabPage { Text = "Drawer Audit" };
        _gridCashDrawer = new GridControl { Dock = DockStyle.Fill };
        _viewCashDrawer = new GridView(_gridCashDrawer);
        SetupStandardGridView(_gridCashDrawer, _viewCashDrawer);
        pageDrawer.Controls.Add(_gridCashDrawer);
        _tabControl.TabPages.Add(pageDrawer);

        // Tab F: Cashier Performance
        var pageCashiers = new XtraTabPage { Text = "Cashier Breakdown" };
        _gridCashiers = new GridControl { Dock = DockStyle.Fill };
        _viewCashiers = new GridView(_gridCashiers);
        SetupStandardGridView(_gridCashiers, _viewCashiers);
        pageCashiers.Controls.Add(_gridCashiers);
        _tabControl.TabPages.Add(pageCashiers);

        // Tab G: Order Type Distribution
        var pageOrderTypes = new XtraTabPage { Text = "Order Types" };
        _gridOrderTypes = new GridControl { Dock = DockStyle.Fill };
        _viewOrderTypes = new GridView(_gridOrderTypes);
        SetupStandardGridView(_gridOrderTypes, _viewOrderTypes);
        pageOrderTypes.Controls.Add(_gridOrderTypes);
        _tabControl.TabPages.Add(pageOrderTypes);

        mainPanel.Controls.Add(_tabControl, 0, 3);

        // 5. Notes
        var pnlNotes = new Panel { Dock = DockStyle.Fill };
        var lblNotes = new LabelControl { Text = "Manager Closing Notes:", Dock = DockStyle.Top };
        _txtNotes = new MemoEdit { Dock = DockStyle.Fill };
        pnlNotes.Controls.Add(_txtNotes);
        pnlNotes.Controls.Add(lblNotes);
        mainPanel.Controls.Add(pnlNotes, 0, 4);

        // 6. Action Buttons
        var pnlButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 4, 0, 0)
        };

        _btnCloseDay = new SimpleButton
        {
            Text = "Close Business Day",
            Height = DesktopDpi.Scale(34, this),
            Width = DesktopDpi.Scale(160, this),
            Appearance = { Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = Color.FromArgb(13, 148, 136), ForeColor = Color.White },
            Cursor = Cursors.Hand,
            Enabled = false
        };
        _btnCloseDay.Click += async (s, e) => await CloseDayAsync();

        _btnCancel = new SimpleButton
        {
            Text = "Cancel",
            Height = DesktopDpi.Scale(34, this),
            Width = DesktopDpi.Scale(90, this),
            DialogResult = DialogResult.Cancel,
            Cursor = Cursors.Hand
        };

        _btnPrintReport = new SimpleButton
        {
            Text = "Print Report",
            Height = DesktopDpi.Scale(34, this),
            Width = DesktopDpi.Scale(110, this),
            Cursor = Cursors.Hand,
            Enabled = false
        };
        _btnPrintReport.Click += (s, e) => PrintDayCloseReport();

        _btnExportPdf = new SimpleButton
        {
            Text = "Export PDF",
            Height = DesktopDpi.Scale(34, this),
            Width = DesktopDpi.Scale(100, this),
            Cursor = Cursors.Hand,
            Enabled = false
        };
        _btnExportPdf.Click += (s, e) => ExportCurrentTabPdf();

        _btnExportExcel = new SimpleButton
        {
            Text = "Export Excel",
            Height = DesktopDpi.Scale(34, this),
            Width = DesktopDpi.Scale(105, this),
            Cursor = Cursors.Hand,
            Enabled = false
        };
        _btnExportExcel.Click += (s, e) => ExportCurrentTabExcel();

        pnlButtons.Controls.Add(_btnCloseDay);
        pnlButtons.Controls.Add(_btnCancel);
        pnlButtons.Controls.Add(_btnExportExcel);
        pnlButtons.Controls.Add(_btnExportPdf);
        pnlButtons.Controls.Add(_btnPrintReport);
        mainPanel.Controls.Add(pnlButtons, 0, 5);

        Controls.Add(mainPanel);
    }

    private void SetupStandardGridView(GridControl grid, GridView view)
    {
        grid.MainView = view;
        view.OptionsBehavior.Editable = false;
        view.OptionsView.ShowGroupPanel = false;
        view.OptionsView.ShowIndicator = false;
        view.OptionsView.EnableAppearanceEvenRow = true;
        view.RowHeight = DesktopDpi.Scale(26, this);
    }

    private LabelControl CreateMetricCard(string label, string initVal, Color valColor, TableLayoutPanel parent, int col, int row)
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple,
            Margin = new Padding(2)
        };
        var lblTitle = new LabelControl
        {
            Text = label,
            Appearance = { Font = new Font("Segoe UI", 7.5F, FontStyle.Regular), ForeColor = Color.FromArgb(100, 116, 139) },
            Location = new Point(6, 4)
        };
        var lblVal = new LabelControl
        {
            Text = initVal,
            Appearance = { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = valColor },
            Location = new Point(6, 20)
        };
        panel.Controls.Add(lblTitle);
        panel.Controls.Add(lblVal);
        parent.Controls.Add(panel, col, row);
        return lblVal;
    }

    private async Task LoadSummaryAsync()
    {
        try
        {
            var summary = await _mediator.Send(new GetBusinessDaySummaryQuery(_branchId, _businessDate));
            if (summary == null)
            {
                _lblBanner.Text = "Failed to load business day summary.";
                _bannerPanel.Appearance.BackColor = Color.FromArgb(254, 242, 242);
                _lblBanner.Appearance.ForeColor = Color.FromArgb(220, 38, 38);
                return;
            }

            BindSummary(summary);
        }
        catch (Exception ex)
        {
            _bannerPanel.Appearance.BackColor = Color.FromArgb(254, 242, 242);
            _lblBanner.Appearance.ForeColor = Color.FromArgb(220, 38, 38);
            _lblBanner.Text = $"Error loading business day summary: {ex.Message}";
            _btnCloseDay.Enabled = false;
        }
    }

    internal void BindSummary(BusinessDaySummaryDto summary)
    {
        _summary = summary ?? throw new ArgumentNullException(nameof(summary));

        // Populate top KPI cards
        _lblGrossSales.Text = CurrencyDisplay.Format(_summary.GrossSales > 0 ? _summary.GrossSales : _summary.TotalSales + _summary.Discounts - _summary.Tax);
        _lblDiscounts.Text = CurrencyDisplay.Format(_summary.Discounts);
        _lblNetSales.Text = CurrencyDisplay.Format(_summary.NetSales > 0 ? _summary.NetSales : _summary.TotalSales);
        _lblTax.Text = CurrencyDisplay.Format(_summary.Tax);

        _lblExpectedCash.Text = CurrencyDisplay.Format(_summary.ExpectedClosingCash > 0 ? _summary.ExpectedClosingCash : _summary.OpeningFloat + _summary.CashSales - _summary.CashRefunds);
        _lblActualCash.Text = CurrencyDisplay.Format(_summary.ActualClosingCash);

        var variance = _summary.TotalShiftVariance;
        _lblCashVariance.Text = CurrencyDisplay.Format(variance);
        _lblCashVariance.Appearance.ForeColor = variance < 0
            ? Color.FromArgb(220, 38, 38)
            : (variance > 0 ? Color.FromArgb(22, 163, 74) : Color.FromArgb(30, 41, 59));

        _lblRefunds.Text = CurrencyDisplay.Format(_summary.Refunds);

        // Tab 1: Shifts
        var allShifts = _summary.OpenShifts.Concat(_summary.ClosedShifts).OrderBy(s => s.ShiftNumber).ToList();
        _gridShifts.DataSource = allShifts;
        FormatShiftsGridColumns();

        // Tab 2: Sales Summary Detail
        _gridSalesMetrics.DataSource = BuildSalesSummaryItems(_summary);
        FormatKeyValueGrid(_viewSalesMetrics);

        // Tab 3: Tax Breakdown
        var taxItems = _summary.TaxBreakdown ?? [];
        _gridTaxBreakdown.DataSource = taxItems.ToList();
        FormatTaxGridColumns();

        // Tab 4: Tender Breakdown
        var tenderItems = _summary.TenderBreakdown ?? [];
        _gridTenders.DataSource = tenderItems.ToList();
        FormatTenderGridColumns();

        // Tab 5: Cash Drawer Audit
        _gridCashDrawer.DataSource = BuildCashDrawerAuditItems(_summary);
        FormatKeyValueGrid(_viewCashDrawer);

        // Tab 6: Cashier Performance
        var cashierItems = _summary.CashierBreakdown ?? [];
        _gridCashiers.DataSource = cashierItems.ToList();
        FormatCashierGridColumns();

        // Tab 7: Order Types
        var orderTypeItems = _summary.OrderTypeBreakdown ?? [];
        _gridOrderTypes.DataSource = orderTypeItems.ToList();
        FormatOrderTypeGridColumns();

        _btnPrintReport.Enabled = true;
        _btnExportPdf.Enabled = true;
        _btnExportExcel.Enabled = true;

        // Set Banner State & Close button readiness
        if (_summary.IsAlreadyClosed)
        {
            _bannerPanel.Appearance.BackColor = Color.FromArgb(240, 253, 244);
            _lblBanner.Appearance.ForeColor = Color.FromArgb(22, 163, 74);
            var closedTime = _summary.ExistingClose != null ? DateTimeDisplay.FormatDateTime(_summary.ExistingClose.ClosedAtUtc) : "-";
            _lblBanner.Text = $"Business day is ALREADY CLOSED (Closed at {closedTime} by {UserDisplayNameHelper.FormatCashierName(_summary.ExistingClose?.ClosedByUserName)}).";
            _btnCloseDay.Enabled = false;
            _btnCloseDay.Text = "Day Already Closed";
        }
        else if (!_summary.CanClose)
        {
            _bannerPanel.Appearance.BackColor = Color.FromArgb(254, 242, 242);
            _lblBanner.Appearance.ForeColor = Color.FromArgb(220, 38, 38);
            _lblBanner.Text = _summary.BlockingReason ?? "Cannot close business day: open shifts exist.";
            _btnCloseDay.Enabled = false;
        }
        else
        {
            _bannerPanel.Appearance.BackColor = Color.FromArgb(240, 249, 255);
            _lblBanner.Appearance.ForeColor = Color.FromArgb(2, 132, 199);
            _lblBanner.Text = "All shifts closed. Ready to perform Day Close.";
            _btnCloseDay.Enabled = true;
        }
    }

    private sealed record FinancialMetricRow(string Metric, string Value, string Category);

    private static List<FinancialMetricRow> BuildSalesSummaryItems(BusinessDaySummaryDto s)
    {
        var gross = s.GrossSales > 0 ? s.GrossSales : s.TotalSales + s.Discounts - s.Tax;
        var net = s.NetSales > 0 ? s.NetSales : s.TotalSales;
        var aov = s.AverageOrderValue > 0 ? s.AverageOrderValue : (s.CompletedOrdersCount > 0 ? net / s.CompletedOrdersCount : (s.TotalOrders > 0 ? net / s.TotalOrders : 0m));

        return
        [
            new("Gross Sales", CurrencyDisplay.Format(gross), "Revenue"),
            new("Item-Level Discounts", CurrencyDisplay.Format(s.ItemDiscounts), "Discounts"),
            new("Order-Level Discounts", CurrencyDisplay.Format(s.OrderDiscounts), "Discounts"),
            new("Total Discounts", CurrencyDisplay.Format(s.Discounts), "Discounts"),
            new("Net Sales (Tax Exclusive Revenue)", CurrencyDisplay.Format(net), "Revenue"),
            new("Total Statutory Sales Tax", CurrencyDisplay.Format(s.Tax), "Taxes"),
            new("Total Compensating Refunds", CurrencyDisplay.Format(s.Refunds), "Refunds"),
            new("Total Orders Created", s.TotalOrders.ToString(), "Operations"),
            new("Completed Orders", s.CompletedOrdersCount > 0 ? s.CompletedOrdersCount.ToString() : s.TotalOrders.ToString(), "Operations"),
            new("Cancelled / Voided Orders", s.CancelledOrdersCount.ToString(), "Operations"),
            new("Average Order Value (AOV)", CurrencyDisplay.Format(aov), "Performance")
        ];
    }

    private static List<FinancialMetricRow> BuildCashDrawerAuditItems(BusinessDaySummaryDto s)
    {
        var expected = s.ExpectedClosingCash > 0 ? s.ExpectedClosingCash : s.OpeningFloat + s.CashSales - s.CashRefunds;
        var variance = s.TotalShiftVariance;
        return
        [
            new("Opening Float (Starting Cash)", CurrencyDisplay.Format(s.OpeningFloat), "Cash Drawer"),
            new("Cash Sales Tendered", CurrencyDisplay.Format(s.CashSales), "Cash Drawer"),
            new("Cash Pay-Ins", CurrencyDisplay.Format(s.CashIn), "Cash Drawer"),
            new("Cash Pay-Outs (-)", CurrencyDisplay.Format(s.CashOut), "Cash Drawer"),
            new("Cash Refunds Disbursed (-)", CurrencyDisplay.Format(s.CashRefunds), "Cash Drawer"),
            new("Expected Closing Cash in Drawer", CurrencyDisplay.Format(expected), "Reconciliation"),
            new("Actual Counted Cash in Drawer", CurrencyDisplay.Format(s.ActualClosingCash), "Reconciliation"),
            new("Cash Drawer Variance (Over / Short)", CurrencyDisplay.Format(variance), "Reconciliation")
        ];
    }

    private void FormatKeyValueGrid(GridView view)
    {
        if (view.Columns.Count >= 3)
        {
            view.Columns[0].Width = 260;
            view.Columns[1].Width = 160;
            view.Columns[2].Width = 140;
            view.Columns[1].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Far;
            view.Columns[1].AppearanceCell.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }
    }

    private void FormatShiftsGridColumns()
    {
        if (_viewShifts.Columns.Count == 0) return;
        if (_viewShifts.Columns["ShiftId"] != null) _viewShifts.Columns["ShiftId"].Visible = false;
        if (_viewShifts.Columns["TerminalId"] != null) _viewShifts.Columns["TerminalId"].Visible = false;
        SetNumericColumnFormat(_viewShifts, "StartingCash");
        SetNumericColumnFormat(_viewShifts, "ExpectedCash");
        SetNumericColumnFormat(_viewShifts, "CountedCash");
        SetNumericColumnFormat(_viewShifts, "CashVariance");
        SetNumericColumnFormat(_viewShifts, "CashRefunds");
        SetNumericColumnFormat(_viewShifts, "CustomerAccountCreditRefunds");
    }

    private void FormatTaxGridColumns()
    {
        if (_viewTaxBreakdown.Columns.Count == 0) return;
        SetNumericColumnFormat(_viewTaxBreakdown, "RatePercentage");
        SetNumericColumnFormat(_viewTaxBreakdown, "TaxableBase");
        SetNumericColumnFormat(_viewTaxBreakdown, "TaxAmount");
    }

    private void FormatTenderGridColumns()
    {
        if (_viewTenders.Columns.Count == 0) return;
        SetNumericColumnFormat(_viewTenders, "TotalAmount");
    }

    private void FormatCashierGridColumns()
    {
        if (_viewCashiers.Columns.Count == 0) return;
        SetNumericColumnFormat(_viewCashiers, "TotalSales");
    }

    private void FormatOrderTypeGridColumns()
    {
        if (_viewOrderTypes.Columns.Count == 0) return;
        SetNumericColumnFormat(_viewOrderTypes, "TotalSales");
    }

    private static void SetNumericColumnFormat(GridView view, string fieldName)
    {
        if (view.Columns[fieldName] != null)
        {
            view.Columns[fieldName].DisplayFormat.FormatType = FormatType.Numeric;
            view.Columns[fieldName].DisplayFormat.FormatString = "n2";
            view.Columns[fieldName].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Far;
        }
    }

    private GridControl? GetActiveGrid() => _tabControl.SelectedTabPageIndex switch
    {
        0 => _gridShifts,
        1 => _gridSalesMetrics,
        2 => _gridTaxBreakdown,
        3 => _gridTenders,
        4 => _gridCashDrawer,
        5 => _gridCashiers,
        6 => _gridOrderTypes,
        _ => _gridShifts
    };

    private string GetActiveGridTitle() => _tabControl.SelectedTabPageIndex switch
    {
        0 => "Cashier Shifts",
        1 => "Sales Summary Breakdown",
        2 => "Tax Breakdown",
        3 => "Payment Tenders",
        4 => "Cash Drawer Audit",
        5 => "Cashier Breakdown",
        6 => "Order Types",
        _ => "Day Close Report"
    };

    private void ExportCurrentTabPdf()
    {
        var grid = GetActiveGrid();
        if (grid == null) return;
        var title = $"{GetActiveGridTitle()} - {_branchName}";
        var subtitle = $"Business Date: {DateTimeDisplay.FormatDate(_businessDate)}";
        GridReportingPrintService.ExportToPdf(grid, title, subtitle, this, landscape: true);
    }

    private void ExportCurrentTabExcel()
    {
        var grid = GetActiveGrid();
        if (grid == null) return;
        var title = $"{GetActiveGridTitle()} - {_branchName}";
        GridReportingPrintService.ExportToExcel(grid, title, this);
    }

    private void PrintDayCloseReport()
    {
        if (_summary == null) return;

        var sb = new StringBuilder();
        var dateStr = DateTimeDisplay.FormatDate(_businessDate);
        sb.AppendLine("========================================");
        sb.AppendLine("    CLOVENT BUSINESS OPERATING SYSTEM   ");
        sb.AppendLine("       BUSINESS DAY CLOSING REPORT      ");
        sb.AppendLine("========================================");
        sb.AppendLine($"Branch:        {_branchName}");
        sb.AppendLine($"Business Date: {dateStr}");
        sb.AppendLine($"Generated:     {DateTimeDisplay.FormatDateTime(DateTimeOffset.UtcNow)}");
        sb.AppendLine(new string('-', 40));

        sb.AppendLine("1. SALES SUMMARY");
        var gross = _summary.GrossSales > 0 ? _summary.GrossSales : _summary.TotalSales + _summary.Discounts - _summary.Tax;
        var net = _summary.NetSales > 0 ? _summary.NetSales : _summary.TotalSales;
        sb.AppendLine($"  Gross Sales:        {CurrencyDisplay.Format(gross)}");
        sb.AppendLine($"  Item Discounts:     {CurrencyDisplay.Format(_summary.ItemDiscounts)}");
        sb.AppendLine($"  Order Discounts:    {CurrencyDisplay.Format(_summary.OrderDiscounts)}");
        sb.AppendLine($"  Total Discounts:    {CurrencyDisplay.Format(_summary.Discounts)}");
        sb.AppendLine($"  Net Sales:          {CurrencyDisplay.Format(net)}");
        sb.AppendLine($"  Completed Orders:   {_summary.CompletedOrdersCount}");
        sb.AppendLine($"  Cancelled Orders:   {_summary.CancelledOrdersCount}");
        sb.AppendLine($"  Refunded Orders:    {_summary.RefundedOrdersCount}");
        sb.AppendLine(new string('-', 40));

        sb.AppendLine("2. TAX BREAKDOWN");
        sb.AppendLine($"  Taxable Sales:      {CurrencyDisplay.Format(_summary.TaxableSales)}");
        sb.AppendLine($"  Tax-Exempt Sales:   {CurrencyDisplay.Format(_summary.TaxExemptSales)}");
        sb.AppendLine($"  Total Tax:          {CurrencyDisplay.Format(_summary.Tax)}");
        if (_summary.TaxBreakdown != null && _summary.TaxBreakdown.Count > 0)
        {
            foreach (var t in _summary.TaxBreakdown)
            {
                sb.AppendLine($"  [{t.TaxCode}] @ {t.RatePercentage:0.##}%: Base={CurrencyDisplay.Format(t.TaxableBase)} Tax={CurrencyDisplay.Format(t.TaxAmount)}");
            }
        }
        sb.AppendLine(new string('-', 40));

        sb.AppendLine("3. PAYMENT SUMMARY");
        sb.AppendLine($"  Cash Sales:         {CurrencyDisplay.Format(_summary.CashSales)}");
        sb.AppendLine($"  Card Sales:         {CurrencyDisplay.Format(_summary.CardSales)}");
        sb.AppendLine($"  Bank Transfers:     {CurrencyDisplay.Format(_summary.BankTransferSales)}");
        sb.AppendLine($"  Digital Wallets:    {CurrencyDisplay.Format(_summary.DigitalSales)}");
        sb.AppendLine($"  Other Tender:       {CurrencyDisplay.Format(_summary.OtherSales)}");
        sb.AppendLine($"  Split Payments:     {_summary.SplitPaymentsCount} ({CurrencyDisplay.Format(_summary.SplitPaymentsTotal)})");
        sb.AppendLine($"  Collections:        {CurrencyDisplay.Format(_summary.TotalSales)}");
        sb.AppendLine($"  Refunds (Total):    {CurrencyDisplay.Format(_summary.Refunds)}");
        sb.AppendLine($"  Receivables AR:     {CurrencyDisplay.Format(_summary.OutstandingReceivables)}");
        sb.AppendLine(new string('-', 40));

        sb.AppendLine("4. CASH DRAWER RECONCILIATION");
        var expected = _summary.ExpectedClosingCash > 0 ? _summary.ExpectedClosingCash : _summary.OpeningFloat + _summary.CashSales - _summary.CashRefunds;
        sb.AppendLine($"  Opening Float:      {CurrencyDisplay.Format(_summary.OpeningFloat)}");
        sb.AppendLine($"  Cash In:            {CurrencyDisplay.Format(_summary.CashIn)}");
        sb.AppendLine($"  Cash Out:           {CurrencyDisplay.Format(_summary.CashOut)}");
        sb.AppendLine($"  Cash Refunds:       {CurrencyDisplay.Format(_summary.CashRefunds)}");
        sb.AppendLine($"  Expected Cash:      {CurrencyDisplay.Format(expected)}");
        sb.AppendLine($"  Counted Cash:       {CurrencyDisplay.Format(_summary.ActualClosingCash)}");
        sb.AppendLine($"  Variance:           {CurrencyDisplay.Format(_summary.TotalShiftVariance)}");
        sb.AppendLine(new string('-', 40));

        sb.AppendLine("5. CASHIER PERFORMANCE");
        if (_summary.CashierBreakdown != null && _summary.CashierBreakdown.Count > 0)
        {
            foreach (var c in _summary.CashierBreakdown)
            {
                sb.AppendLine($"  {c.CashierName}: {c.OrderCount} orders, Sales: {CurrencyDisplay.Format(c.TotalSales)}");
            }
        }
        sb.AppendLine(new string('-', 40));

        sb.AppendLine("6. ORDER TYPE DISTRIBUTION");
        if (_summary.OrderTypeBreakdown != null && _summary.OrderTypeBreakdown.Count > 0)
        {
            foreach (var ot in _summary.OrderTypeBreakdown)
            {
                sb.AppendLine($"  {ot.OrderType}: {ot.OrderCount} orders, Sales: {CurrencyDisplay.Format(ot.TotalSales)}");
            }
        }
        sb.AppendLine("========================================");

        using var preview = new ReceiptPreviewForm(sb.ToString());
        preview.ShowDialog(this);
    }

    private async Task CloseDayAsync()
    {
        if (_summary == null || !_summary.CanClose)
            return;

        var confirm = XtraMessageBox.Show(
            this,
            $"Are you sure you want to close Business Day {DateTimeDisplay.FormatDate(_businessDate)} for {_branchName}?\n\n" +
            $"Total Shifts: {_summary.TotalShifts}\n" +
            $"Total Net Sales: {CurrencyDisplay.Format(_summary.TotalSales)}\n" +
            $"Cash Refunds (-): {CurrencyDisplay.Format(_summary.CashRefunds)}\n" +
            $"Account Credit Refunds: {CurrencyDisplay.Format(_summary.CustomerAccountCreditRefunds)}\n" +
            $"Shift Cash Variance: {CurrencyDisplay.Format(_summary.TotalShiftVariance)}\n\n" +
            "This action finalizes and locks the business day.",
            "Confirm Business Day Close",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        if (_currentSession?.UserId is null || !_currentSession.IsAuthenticated)
        {
            XtraMessageBox.Show(this, "No authenticated manager session found to perform Day Close.", "Authentication Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnCloseDay.Enabled = false;
        try
        {
            var command = new CloseBusinessDayCommand(
                _branchId,
                _businessDate,
                _currentSession.UserId.Value,
                UserDisplayNameHelper.GetCurrentCashierDisplayName(_currentSession, "Manager"),
                _txtNotes.Text.Trim());

            ClosedResult = await _mediator.Send(command);

            XtraMessageBox.Show(
                this,
                $"Business Day {_businessDate:yyyy-MM-dd} successfully closed.",
                "Day Close Completed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(
                this,
                $"Failed to close business day: {ex.Message}",
                "Day Close Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _btnCloseDay.Enabled = true;
        }
    }
}
