using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Inventory.Application.Forecasting.Dtos;
using Clovent.Inventory.Application.Forecasting.Queries;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.CashierAudits.Commands;
using Clovent.Restaurant.Application.CashierAudits.Dtos;
using Clovent.Restaurant.Application.CashierAudits.Queries;
using DevExpress.Sparkline;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Audit;

/// <summary>
/// DevExpress-backed back-office analytics dashboard for automated cashier behavior anomaly detection,
/// shrinkage risk scoring, forensic suspect transaction drill-downs, and predictive inventory burn-rate depletion forecasting.
/// Registered under ManagerPanel (<c>menu.audit.analytics</c>). Certified PerMonitorV2 High-DPI compliant.
/// </summary>
[DesignerCategory("Code")]
public sealed class CashierAuditAnalyticsControl : XtraUserControl
{
    private readonly IServiceScope? _scope;
    private readonly IMediator? _mediator;
    private readonly ICurrentSession? _currentSession;
    private readonly ILogger<CashierAuditAnalyticsControl>? _logger;
    private readonly IWarehouseRepository? _warehouseRepository;

    private readonly TableLayoutPanel _mainLayout = new();
    private readonly PanelControl _headerPanel = new();
    private readonly TableLayoutPanel _kpiCardLayout = new();
    private readonly XtraTabControl _tabControl = new();

    // Filter controls
    private readonly ComboBoxEdit _cboPeriod = new();
    private readonly DateEdit _dtFrom = new();
    private readonly DateEdit _dtTo = new();
    private readonly SimpleButton _btnRefresh = new();
    private readonly SimpleButton _btnRunScan = new();
    private readonly SimpleButton _btnRunForecast = new();

    // KPI Card Labels
    private readonly LabelControl _lblKpiAlertsVal = new();
    private readonly LabelControl _lblKpiRiskVal = new();
    private readonly LabelControl _lblKpiVoidsVal = new();
    private readonly LabelControl _lblKpiStockoutVal = new();

    // Tab 1: Cashier Risk Scores Grid
    private readonly GridControl _gridRiskScores = new();
    private readonly GridView _viewRiskScores = new();

    // Tab 2: Anomaly Alerts & Drill-down
    private readonly SplitContainerControl _splitAlerts = new();
    private readonly GridControl _gridAlerts = new();
    private readonly GridView _viewAlerts = new();
    private readonly MemoEdit _txtSuspectDetails = new();
    private readonly SimpleButton _btnMarkReviewed = new();
    private readonly SimpleButton _btnDismissAlert = new();

    // Tab 3: Predictive Inventory Forecasting
    private readonly GridControl _gridInventoryForecast = new();
    private readonly GridView _viewInventoryForecast = new();

    private Guid? _selectedWarehouseId;
    private CashierAuditAlertDto? _selectedAlert;

    /// <summary>Runtime constructor using dependency injection.</summary>
    public CashierAuditAnalyticsControl(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _logger = _scope.ServiceProvider.GetRequiredService<ILogger<CashierAuditAnalyticsControl>>();
        _warehouseRepository = _scope.ServiceProvider.GetService<IWarehouseRepository>();
        _currentSession = currentSession;

        InitializeLayout();

        if (!DesignModeHelper.IsInDesignMode)
        {
            Load += async (_, _) => await OnControlLoadedAsync();
        }
    }

    /// <summary>Designer-only constructor for Visual Studio Designer.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Designer only")]
    public CashierAuditAnalyticsControl()
    {
        InitializeLayout();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 249, 250);

        _mainLayout.Dock = DockStyle.Fill;
        _mainLayout.ColumnCount = 1;
        _mainLayout.RowCount = 3;
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(56, this)));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(104, this)));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(_mainLayout);

        BuildHeaderBar();
        BuildKpiCards();
        BuildTabPanels();

        _mainLayout.Controls.Add(_headerPanel, 0, 0);
        _mainLayout.Controls.Add(_kpiCardLayout, 0, 1);
        _mainLayout.Controls.Add(_tabControl, 0, 2);
    }

    private void BuildHeaderBar()
    {
        _headerPanel.Dock = DockStyle.Fill;
        _headerPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _headerPanel.BackColor = Color.White;

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 1,
            Padding = new Padding(DesktopDpi.Scale(12, this), DesktopDpi.Scale(8, this), DesktopDpi.Scale(12, this), DesktopDpi.Scale(8, this))
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // Title
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Period
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // From
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // To
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Refresh
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Scan
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Forecast

        var lblTitle = new LabelControl
        {
            Text = "Cashier Audit Anomaly & Predictive Inventory Engine",
            Dock = DockStyle.Fill,
            Anchor = AnchorStyles.Left,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 37, 41)
        };
        lblTitle.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        headerLayout.Controls.Add(lblTitle, 0, 0);

        _cboPeriod.Properties.Items.AddRange(["Today", "Yesterday", "Last 7 Days", "Last 30 Days"]);
        _cboPeriod.SelectedIndex = 2; // Last 7 Days
        _cboPeriod.Width = DesktopDpi.Scale(120, this);
        _cboPeriod.Height = DesktopStyle.ToolbarControlHeight;
        _cboPeriod.SelectedIndexChanged += (_, _) => OnPeriodChanged();
        headerLayout.Controls.Add(_cboPeriod, 1, 0);

        _dtFrom.Width = DesktopDpi.Scale(105, this);
        _dtFrom.Height = DesktopStyle.ToolbarControlHeight;
        _dtFrom.DateTime = DateTime.Today.AddDays(-7);
        headerLayout.Controls.Add(_dtFrom, 2, 0);

        _dtTo.Width = DesktopDpi.Scale(105, this);
        _dtTo.Height = DesktopStyle.ToolbarControlHeight;
        _dtTo.DateTime = DateTime.Today;
        headerLayout.Controls.Add(_dtTo, 3, 0);

        _btnRefresh.Text = "Refresh";
        _btnRefresh.Width = DesktopDpi.Scale(90, this);
        _btnRefresh.Height = DesktopStyle.ToolbarControlHeight;
        _btnRefresh.Click += async (_, _) => await LoadAllDataAsync();
        headerLayout.Controls.Add(_btnRefresh, 4, 0);

        _btnRunScan.Text = "Run Anomaly Scan";
        _btnRunScan.Width = DesktopDpi.Scale(140, this);
        _btnRunScan.Height = DesktopStyle.ToolbarControlHeight;
        _btnRunScan.Appearance.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _btnRunScan.Appearance.ForeColor = Color.DarkRed;
        _btnRunScan.Click += async (_, _) => await RunAnomalyScanAsync();
        headerLayout.Controls.Add(_btnRunScan, 5, 0);

        _btnRunForecast.Text = "Run Inventory Forecast";
        _btnRunForecast.Width = DesktopDpi.Scale(150, this);
        _btnRunForecast.Height = DesktopStyle.ToolbarControlHeight;
        _btnRunForecast.Click += async (_, _) => await LoadInventoryForecastAsync();
        headerLayout.Controls.Add(_btnRunForecast, 6, 0);

        _headerPanel.Controls.Add(headerLayout);
    }

    private void BuildKpiCards()
    {
        _kpiCardLayout.Dock = DockStyle.Fill;
        _kpiCardLayout.ColumnCount = 4;
        _kpiCardLayout.RowCount = 1;
        _kpiCardLayout.Padding = new Padding(DesktopDpi.Scale(12, this), DesktopDpi.Scale(6, this), DesktopDpi.Scale(12, this), DesktopDpi.Scale(6, this));

        for (int i = 0; i < 4; i++)
            _kpiCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        _kpiCardLayout.Controls.Add(CreateKpiCard("ACTIVE AUDIT ALERTS", _lblKpiAlertsVal, Color.FromArgb(220, 53, 69)), 0, 0);
        _kpiCardLayout.Controls.Add(CreateKpiCard("MAX CASHIER RISK SCORE", _lblKpiRiskVal, Color.FromArgb(253, 126, 20)), 1, 0);
        _kpiCardLayout.Controls.Add(CreateKpiCard("VOIDS & OVERRIDES DETECTED", _lblKpiVoidsVal, Color.FromArgb(13, 110, 253)), 2, 0);
        _kpiCardLayout.Controls.Add(CreateKpiCard("STOCKOUT RISKS BEFORE GRN", _lblKpiStockoutVal, Color.FromArgb(111, 66, 193)), 3, 0);
    }

    private PanelControl CreateKpiCard(string caption, LabelControl valueLabel, Color accentColor)
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple,
            BackColor = Color.White
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(DesktopDpi.Scale(10, this))
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));

        var lblCaption = new LabelControl
        {
            Text = caption,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(108, 117, 125),
            Dock = DockStyle.Fill
        };

        valueLabel.Text = "0";
        valueLabel.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
        valueLabel.ForeColor = accentColor;
        valueLabel.Dock = DockStyle.Fill;

        layout.Controls.Add(lblCaption, 0, 0);
        layout.Controls.Add(valueLabel, 0, 1);
        panel.Controls.Add(layout);
        return panel;
    }

    private void BuildTabPanels()
    {
        _tabControl.Dock = DockStyle.Fill;
        _tabControl.Padding = new Padding(DesktopDpi.Scale(12, this), DesktopDpi.Scale(4, this), DesktopDpi.Scale(12, this), DesktopDpi.Scale(12, this));

        // Tab 1: Cashier Risk Scores & Heatmap
        var pageRisk = new XtraTabPage { Text = "Cashier Risk Scores & Heatmap" };
        ConfigureRiskGrid();
        pageRisk.Controls.Add(_gridRiskScores);
        _tabControl.TabPages.Add(pageRisk);

        // Tab 2: Anomaly Alerts & Drill-Down
        var pageAlerts = new XtraTabPage { Text = "Suspect Transactions & Audit Alerts" };
        ConfigureAlertsSplit();
        pageAlerts.Controls.Add(_splitAlerts);
        _tabControl.TabPages.Add(pageAlerts);

        // Tab 3: Predictive Inventory Depletion
        var pageForecast = new XtraTabPage { Text = "Predictive Inventory Depletion & Stockout Forecast" };
        ConfigureInventoryGrid();
        pageForecast.Controls.Add(_gridInventoryForecast);
        _tabControl.TabPages.Add(pageForecast);
    }

    private void ConfigureRiskGrid()
    {
        _gridRiskScores.Dock = DockStyle.Fill;
        _gridRiskScores.MainView = _viewRiskScores;
        _gridRiskScores.ViewCollection.Add(_viewRiskScores);

        _viewRiskScores.OptionsBehavior.Editable = false;
        _viewRiskScores.OptionsView.ShowGroupPanel = false;
        _viewRiskScores.RowHeight = DesktopDpi.Scale(30, this);
        _viewRiskScores.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        var colCashier = _viewRiskScores.Columns.AddVisible("CashierName", "Cashier Name");
        colCashier.Width = DesktopDpi.Scale(160, this);

        var colRiskScore = _viewRiskScores.Columns.AddVisible("OverallRiskScore", "Risk Score (0-100)");
        colRiskScore.Width = DesktopDpi.Scale(120, this);

        var colRiskLevel = _viewRiskScores.Columns.AddVisible("RiskLevel", "Risk Level");
        colRiskLevel.Width = DesktopDpi.Scale(100, this);

        var colShifts = _viewRiskScores.Columns.AddVisible("TotalShiftsObserved", "Shifts");
        colShifts.Width = DesktopDpi.Scale(70, this);

        var colActiveAlerts = _viewRiskScores.Columns.AddVisible("ActiveAlertsCount", "Active Alerts");
        colActiveAlerts.Width = DesktopDpi.Scale(90, this);

        var colVoids = _viewRiskScores.Columns.AddVisible("ExcessiveVoidAlertsCount", "Void Anomalies");
        colVoids.Width = DesktopDpi.Scale(100, this);

        var colOverrides = _viewRiskScores.Columns.AddVisible("ManagerOverridesCount", "Overrides");
        colOverrides.Width = DesktopDpi.Scale(80, this);

        var colDrawers = _viewRiskScores.Columns.AddVisible("UnlinkedDrawerCount", "Drawer Kicks");
        colDrawers.Width = DesktopDpi.Scale(90, this);

        var colDiscounts = _viewRiskScores.Columns.AddVisible("DiscountClusterCount", "Discount Clusters");
        colDiscounts.Width = DesktopDpi.Scale(110, this);

        var colVoidAmount = _viewRiskScores.Columns.AddVisible("TotalVoidAmount", "Suspect Void Amt");
        colVoidAmount.DisplayFormat.FormatType = FormatType.Numeric;
        colVoidAmount.DisplayFormat.FormatString = "N2";
        colVoidAmount.Width = DesktopDpi.Scale(110, this);

        // Sparkline column
        var colSparkline = _viewRiskScores.Columns.AddVisible("RiskTrendSparkline", "7-Day Risk Trend");
        colSparkline.Width = DesktopDpi.Scale(140, this);

        var sparklineEditor = new RepositoryItemSparklineEdit();
        var lineView = new LineSparklineView
        {
            HighlightMaxPoint = true,
            HighlightMinPoint = true,
            Color = Color.FromArgb(220, 53, 69)
        };
        sparklineEditor.View = lineView;
        _gridRiskScores.RepositoryItems.Add(sparklineEditor);
        colSparkline.ColumnEdit = sparklineEditor;

        // Anomaly Heatmap cell coloring
        _viewRiskScores.RowCellStyle += (s, e) =>
        {
            if (e.Column.FieldName == "OverallRiskScore")
            {
                if (decimal.TryParse(e.CellValue?.ToString(), out var score))
                {
                    if (score >= 75.0m)
                    {
                        e.Appearance.BackColor = Color.FromArgb(255, 230, 230);
                        e.Appearance.ForeColor = Color.FromArgb(180, 0, 0);
                        e.Appearance.Font = new Font(_viewRiskScores.Appearance.Row.Font, FontStyle.Bold);
                    }
                    else if (score >= 50.0m)
                    {
                        e.Appearance.BackColor = Color.FromArgb(255, 243, 205);
                        e.Appearance.ForeColor = Color.FromArgb(160, 90, 0);
                        e.Appearance.Font = new Font(_viewRiskScores.Appearance.Row.Font, FontStyle.Bold);
                    }
                    else if (score >= 25.0m)
                    {
                        e.Appearance.BackColor = Color.FromArgb(255, 250, 230);
                        e.Appearance.ForeColor = Color.FromArgb(140, 110, 0);
                    }
                    else
                    {
                        e.Appearance.BackColor = Color.FromArgb(235, 247, 238);
                        e.Appearance.ForeColor = Color.FromArgb(25, 135, 84);
                    }
                }
            }
            else if (e.Column.FieldName == "RiskLevel")
            {
                var level = e.CellValue?.ToString();
                if (level == "Critical")
                {
                    e.Appearance.ForeColor = Color.DarkRed;
                    e.Appearance.Font = new Font(_viewRiskScores.Appearance.Row.Font, FontStyle.Bold);
                }
                else if (level == "High")
                {
                    e.Appearance.ForeColor = Color.FromArgb(210, 100, 0);
                    e.Appearance.Font = new Font(_viewRiskScores.Appearance.Row.Font, FontStyle.Bold);
                }
            }
        };
    }

    private void ConfigureAlertsSplit()
    {
        _splitAlerts.Dock = DockStyle.Fill;
        _splitAlerts.Horizontal = false; // Vertical split: top grid, bottom drilldown
        _splitAlerts.SplitterPosition = DesktopDpi.Scale(280, this);

        // Top: Alerts Grid
        _gridAlerts.Dock = DockStyle.Fill;
        _gridAlerts.MainView = _viewAlerts;
        _gridAlerts.ViewCollection.Add(_viewAlerts);

        _viewAlerts.OptionsBehavior.Editable = false;
        _viewAlerts.OptionsView.ShowGroupPanel = false;
        _viewAlerts.RowHeight = DesktopDpi.Scale(28, this);
        _viewAlerts.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        var colDate = _viewAlerts.Columns.AddVisible("DetectedAtUtc", "Detected At");
        colDate.DisplayFormat.FormatType = FormatType.DateTime;
        colDate.DisplayFormat.FormatString = "g";
        colDate.Width = DesktopDpi.Scale(130, this);

        var colCashier = _viewAlerts.Columns.AddVisible("CashierName", "Cashier");
        colCashier.Width = DesktopDpi.Scale(130, this);

        var colType = _viewAlerts.Columns.AddVisible("AnomalyType", "Anomaly Type");
        colType.Width = DesktopDpi.Scale(180, this);

        var colSev = _viewAlerts.Columns.AddVisible("Severity", "Severity");
        colSev.Width = DesktopDpi.Scale(80, this);

        var colScore = _viewAlerts.Columns.AddVisible("RiskScore", "Score");
        colScore.Width = DesktopDpi.Scale(70, this);

        var colDesc = _viewAlerts.Columns.AddVisible("Description", "Description");
        colDesc.Width = DesktopDpi.Scale(300, this);

        var colStatus = _viewAlerts.Columns.AddVisible("Status", "Status");
        colStatus.Width = DesktopDpi.Scale(90, this);

        _viewAlerts.RowCellStyle += (s, e) =>
        {
            if (e.Column.FieldName == "Severity")
            {
                var sev = e.CellValue?.ToString();
                if (sev == "Critical")
                {
                    e.Appearance.ForeColor = Color.DarkRed;
                    e.Appearance.Font = new Font(_viewAlerts.Appearance.Row.Font, FontStyle.Bold);
                }
                else if (sev == "High")
                {
                    e.Appearance.ForeColor = Color.OrangeRed;
                    e.Appearance.Font = new Font(_viewAlerts.Appearance.Row.Font, FontStyle.Bold);
                }
            }
        };

        _viewAlerts.FocusedRowChanged += (_, e) =>
        {
            if (_viewAlerts.GetRow(e.FocusedRowHandle) is CashierAuditAlertDto alert)
            {
                _selectedAlert = alert;
                DisplayDrillDownDetails(alert);
            }
        };

        _splitAlerts.Panel1.Controls.Add(_gridAlerts);

        // Bottom: Drill-Down Forensic Detail Panel
        var detailPanel = new PanelControl { Dock = DockStyle.Fill };
        var detailLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(DesktopDpi.Scale(8, this))
        };
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(28, this)));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(38, this)));

        var lblDetailHeader = new LabelControl
        {
            Text = "Forensic Drill-Down: Suspect Transaction & Cashier Activity Evidence",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 58, 64)
        };
        detailLayout.Controls.Add(lblDetailHeader, 0, 0);

        _txtSuspectDetails.Dock = DockStyle.Fill;
        _txtSuspectDetails.Properties.ReadOnly = true;
        _txtSuspectDetails.Properties.Appearance.Font = new Font("Consolas", 9f);
        detailLayout.Controls.Add(_txtSuspectDetails, 0, 1);

        var actionLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        _btnMarkReviewed.Text = "Mark Reviewed";
        _btnMarkReviewed.Width = DesktopDpi.Scale(120, this);
        _btnMarkReviewed.Height = DesktopStyle.ToolbarControlHeight;
        _btnMarkReviewed.Click += async (_, _) => await HandleReviewAlertAsync(false);

        _btnDismissAlert.Text = "Dismiss Exception";
        _btnDismissAlert.Width = DesktopDpi.Scale(130, this);
        _btnDismissAlert.Height = DesktopStyle.ToolbarControlHeight;
        _btnDismissAlert.Click += async (_, _) => await HandleReviewAlertAsync(true);

        actionLayout.Controls.Add(_btnMarkReviewed);
        actionLayout.Controls.Add(_btnDismissAlert);
        detailLayout.Controls.Add(actionLayout, 0, 2);

        detailPanel.Controls.Add(detailLayout);
        _splitAlerts.Panel2.Controls.Add(detailPanel);
    }

    private void ConfigureInventoryGrid()
    {
        _gridInventoryForecast.Dock = DockStyle.Fill;
        _gridInventoryForecast.MainView = _viewInventoryForecast;
        _gridInventoryForecast.ViewCollection.Add(_viewInventoryForecast);

        _viewInventoryForecast.OptionsBehavior.Editable = false;
        _viewInventoryForecast.OptionsView.ShowGroupPanel = false;
        _viewInventoryForecast.RowHeight = DesktopDpi.Scale(28, this);
        _viewInventoryForecast.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        var colSku = _viewInventoryForecast.Columns.AddVisible("Sku", "SKU / Ingredient");
        colSku.Width = DesktopDpi.Scale(130, this);

        var colAvail = _viewInventoryForecast.Columns.AddVisible("QuantityAvailable", "Stock Avail");
        colAvail.DisplayFormat.FormatType = FormatType.Numeric;
        colAvail.DisplayFormat.FormatString = "N2";
        colAvail.Width = DesktopDpi.Scale(85, this);

        var colMin = _viewInventoryForecast.Columns.AddVisible("MinimumStock", "Min Stock");
        colMin.DisplayFormat.FormatType = FormatType.Numeric;
        colMin.DisplayFormat.FormatString = "N2";
        colMin.Width = DesktopDpi.Scale(80, this);

        var colBurn = _viewInventoryForecast.Columns.AddVisible("BaseHourlyBurnRate", "Burn Rate/Hr");
        colBurn.DisplayFormat.FormatType = FormatType.Numeric;
        colBurn.DisplayFormat.FormatString = "F3";
        colBurn.Width = DesktopDpi.Scale(95, this);

        var colHours = _viewInventoryForecast.Columns.AddVisible("HoursUntilDepletion", "Hours Left");
        colHours.DisplayFormat.FormatType = FormatType.Numeric;
        colHours.DisplayFormat.FormatString = "F1";
        colHours.Width = DesktopDpi.Scale(85, this);

        var colDepTime = _viewInventoryForecast.Columns.AddVisible("ProjectedDepletionUtc", "Projected Depletion");
        colDepTime.DisplayFormat.FormatType = FormatType.DateTime;
        colDepTime.DisplayFormat.FormatString = "g";
        colDepTime.Width = DesktopDpi.Scale(135, this);

        var colGrnTime = _viewInventoryForecast.Columns.AddVisible("NextScheduledGrnUtc", "Next GRN Arrival");
        colGrnTime.DisplayFormat.FormatType = FormatType.DateTime;
        colGrnTime.DisplayFormat.FormatString = "g";
        colGrnTime.Width = DesktopDpi.Scale(135, this);

        var colDepBeforeGrn = _viewInventoryForecast.Columns.AddVisible("IsDepletedBeforeGrn", "Deplete Pre-GRN?");
        colDepBeforeGrn.Width = DesktopDpi.Scale(110, this);

        var colDeficit = _viewInventoryForecast.Columns.AddVisible("ProjectedDeficitAtGrn", "Deficit @ GRN");
        colDeficit.DisplayFormat.FormatType = FormatType.Numeric;
        colDeficit.DisplayFormat.FormatString = "N2";
        colDeficit.Width = DesktopDpi.Scale(95, this);

        var colReorder = _viewInventoryForecast.Columns.AddVisible("RecommendedReorderQuantity", "Reorder Qty");
        colReorder.DisplayFormat.FormatType = FormatType.Numeric;
        colReorder.DisplayFormat.FormatString = "N2";
        colReorder.Width = DesktopDpi.Scale(95, this);

        var colUrgency = _viewInventoryForecast.Columns.AddVisible("Urgency", "Urgency");
        colUrgency.Width = DesktopDpi.Scale(90, this);

        // Highlight items that deplete before GRN
        _viewInventoryForecast.RowCellStyle += (s, e) =>
        {
            if (e.Column.FieldName == "IsDepletedBeforeGrn")
            {
                if (e.CellValue is true)
                {
                    e.Appearance.BackColor = Color.FromArgb(255, 230, 230);
                    e.Appearance.ForeColor = Color.DarkRed;
                    e.Appearance.Font = new Font(_viewInventoryForecast.Appearance.Row.Font, FontStyle.Bold);
                }
            }
            else if (e.Column.FieldName == "Urgency")
            {
                var urgency = e.CellValue?.ToString();
                if (urgency == "Critical")
                {
                    e.Appearance.ForeColor = Color.DarkRed;
                    e.Appearance.Font = new Font(_viewInventoryForecast.Appearance.Row.Font, FontStyle.Bold);
                }
                else if (urgency == "High")
                {
                    e.Appearance.ForeColor = Color.FromArgb(210, 100, 0);
                    e.Appearance.Font = new Font(_viewInventoryForecast.Appearance.Row.Font, FontStyle.Bold);
                }
            }
        };
    }

    private void DisplayDrillDownDetails(CashierAuditAlertDto alert)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"ALERT IDENTIFIER: {alert.Id}");
        sb.AppendLine($"CASHIER:          {alert.CashierName}");
        sb.AppendLine($"ANOMALY PATTERN:  {alert.AnomalyType}");
        sb.AppendLine($"SEVERITY LEVEL:   {alert.Severity} (Risk Score: {alert.RiskScore:F1} / 100)");
        sb.AppendLine($"DETECTED AT:      {BusinessDateTimeFormatter.Format(alert.DetectedAtUtc)}");
        sb.AppendLine($"STATUS:           {alert.Status}");
        if (!string.IsNullOrWhiteSpace(alert.ReviewedBy))
        {
            sb.AppendLine($"REVIEWED BY:      {alert.ReviewedBy} (At {alert.ReviewedAtUtc:g})");
            sb.AppendLine($"RESOLUTION NOTES: {alert.ResolutionNotes}");
        }
        sb.AppendLine();
        sb.AppendLine("--- NARRATIVE SUMMARY ---");
        sb.AppendLine(alert.Description);
        sb.AppendLine();
        sb.AppendLine("--- FORENSIC EVIDENCE & SUSPECT PAYLOAD ---");

        if (!string.IsNullOrWhiteSpace(alert.SuspectDetailsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(alert.SuspectDetailsJson);
                sb.AppendLine(JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                sb.AppendLine(alert.SuspectDetailsJson);
            }
        }
        else
        {
            sb.AppendLine("No structured suspect payload attached.");
        }

        _txtSuspectDetails.Text = sb.ToString();
    }

    private async Task OnControlLoadedAsync()
    {
        if (_warehouseRepository != null)
        {
            var warehouses = await _warehouseRepository.GetAllAsync();
            _selectedWarehouseId = warehouses.FirstOrDefault()?.Id.Value;
        }

        await LoadAllDataAsync();
    }

    private void OnPeriodChanged()
    {
        var now = DateTime.Today;
        switch (_cboPeriod.SelectedIndex)
        {
            case 0: // Today
                _dtFrom.DateTime = now;
                _dtTo.DateTime = now;
                break;
            case 1: // Yesterday
                _dtFrom.DateTime = now.AddDays(-1);
                _dtTo.DateTime = now.AddDays(-1);
                break;
            case 2: // Last 7 Days
                _dtFrom.DateTime = now.AddDays(-7);
                _dtTo.DateTime = now;
                break;
            case 3: // Last 30 Days
                _dtFrom.DateTime = now.AddDays(-30);
                _dtTo.DateTime = now;
                break;
        }
    }

    private async Task LoadAllDataAsync()
    {
        if (_mediator == null) return;

        try
        {
            var from = new DateTimeOffset(_dtFrom.DateTime.Date, TimeSpan.Zero);
            var to = new DateTimeOffset(_dtTo.DateTime.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero);

            // 1. Load alerts
            var alerts = await _mediator.Send(new GetCashierAuditAlertsQuery(from, to));
            _gridAlerts.DataSource = alerts;

            if (alerts.Count > 0)
            {
                _selectedAlert = alerts[0];
                DisplayDrillDownDetails(_selectedAlert);
            }
            else
            {
                _txtSuspectDetails.Text = "No audit alerts in the selected range.";
            }

            // 2. Load risk rankings
            var riskScores = await _mediator.Send(new GetCashierRiskScoresQuery(from, to));
            _gridRiskScores.DataSource = riskScores;

            // Update KPI cards
            var activeCount = alerts.Count(a => a.Status == "Active" || a.Status == "Investigating");
            _lblKpiAlertsVal.Text = activeCount.ToString();

            var maxRisk = riskScores.Count > 0 ? riskScores.Max(r => r.OverallRiskScore) : 0m;
            _lblKpiRiskVal.Text = $"{maxRisk:F1}";

            var voidsCount = alerts.Count(a => a.AnomalyType.Contains("Void") || a.AnomalyType.Contains("Override"));
            _lblKpiVoidsVal.Text = voidsCount.ToString();

            // 3. Load predictive inventory
            await LoadInventoryForecastAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed loading cashier audit analytics.");
        }
    }

    private async Task RunAnomalyScanAsync()
    {
        if (_mediator == null) return;

        try
        {
            var from = new DateTimeOffset(_dtFrom.DateTime.Date, TimeSpan.Zero);
            var to = new DateTimeOffset(_dtTo.DateTime.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero);

            Cursor = Cursors.WaitCursor;
            var result = await _mediator.Send(new RunCashierAuditAnalysisCommand(from, to));

            XtraMessageBox.Show(
                $"Audit scan complete!\n\nShifts Scanned: {result.TotalShiftsScanned}\nOrders Scanned: {result.TotalOrdersScanned}\nNew Alerts Generated: {result.NewAlertsGenerated}\nExisting Alerts: {result.ExistingAlertsPreserved}",
                "Automated Audit Scanner",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            await LoadAllDataAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed executing anomaly scan.");
            XtraMessageBox.Show($"Audit scan failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private async Task LoadInventoryForecastAsync()
    {
        if (_mediator == null) return;

        try
        {
            var targetWh = _selectedWarehouseId ?? Guid.Empty;
            if (targetWh == Guid.Empty && _warehouseRepository != null)
            {
                var warehouses = await _warehouseRepository.GetAllAsync();
                targetWh = warehouses.FirstOrDefault()?.Id.Value ?? Guid.Empty;
                _selectedWarehouseId = targetWh;
            }

            if (targetWh != Guid.Empty)
            {
                var forecast = await _mediator.Send(new GetInventoryBurnRatesQuery(targetWh));
                _gridInventoryForecast.DataSource = forecast;

                var stockoutPreGrnCount = forecast.Count(f => f.IsDepletedBeforeGrn);
                _lblKpiStockoutVal.Text = stockoutPreGrnCount.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed loading inventory forecast.");
        }
    }

    private async Task HandleReviewAlertAsync(bool isDismiss)
    {
        if (_selectedAlert == null || _mediator == null)
        {
            XtraMessageBox.Show("Please select an alert first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var promptTitle = isDismiss ? "Dismiss Alert Exception" : "Mark Alert Reviewed";
        var promptMsg = isDismiss ? "Enter reason for dismissal:" : "Enter manager resolution notes:";
        var note = XtraInputBox.Show(promptMsg, promptTitle, "Manager reviewed and verified.");

        if (string.IsNullOrWhiteSpace(note)) return;

        var managerName = _currentSession?.UserName ?? "Store Manager";

        try
        {
            var updated = await _mediator.Send(new ReviewCashierAuditAlertCommand(
                _selectedAlert.Id,
                managerName,
                note,
                isDismiss));

            XtraMessageBox.Show("Alert status updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadAllDataAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed updating alert review.");
            XtraMessageBox.Show($"Failed updating alert: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
