using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Printing;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Display model for installed thermal/document printers and live Windows spoolers.
/// </summary>
public sealed class PrinterHealthDisplayItem
{
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string SystemPrinterName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string HardwareCondition { get; set; } = "Ready";
    public bool IsReady { get; set; } = true;
    public int SpoolerQueueDepth { get; set; }
    public string CircuitBreakerState { get; set; } = "Closed";
    public int FailureCount { get; set; }
    public string LastChecked { get; set; } = string.Empty;
}

/// <summary>
/// Display model for quarantined print jobs awaiting hardware recovery.
/// </summary>
public sealed class QuarantinedJobDisplayItem
{
    public Guid JobId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string TargetPrinter { get; set; } = string.Empty;
    public string QuarantinedAt { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public string LastRetryError { get; set; } = string.Empty;
}

/// <summary>
/// Back-Office Peripheral Health Monitoring &amp; Automatic Fallback Spooling Control.
/// Visualizes live hardware connectivity, physical faults (offline, paper out, cover open, cutter error),
/// Windows print spooler queue depths, peripheral circuit breaker states, and provides triggers for
/// authorized diagnostic test printing and automated recovery of quarantined receipt jobs.
/// Fully certified for PerMonitorV2 High-DPI scaling and Visual Studio Designer safety.
/// </summary>
[DesignerCategory("Code")]
public sealed class PrinterHardwareHealthControl : XtraUserControl
{
    private readonly PrinterManagementService? _printerService;
    private readonly IPrintJobQuarantineStore? _quarantineStore;
    private readonly ICircuitBreakerRegistry? _circuitBreakerRegistry;
    private readonly ILogger<PrinterHardwareHealthControl>? _logger;

    // Header Summary Cards
    private readonly PanelControl _pnlSummary = new();
    private readonly LabelControl _lblHealthBanner = new();
    private readonly LabelControl _lblPrinterCountCard = new();
    private readonly LabelControl _lblSpoolerQueueCard = new();
    private readonly LabelControl _lblQuarantineCountCard = new();
    private readonly LabelControl _lblCircuitBreakersCard = new();

    // Toolbar Controls
    private readonly PanelControl _pnlToolbar = new();
    private readonly SimpleButton _btnRefresh = new();
    private readonly SimpleButton _btnTestPrint = new();
    private readonly SimpleButton _btnRetryAllQuarantined = new();
    private readonly SimpleButton _btnRetrySelectedJob = new();
    private readonly SimpleButton _btnDeleteQuarantinedJob = new();
    private readonly SimpleButton _btnResetBreakers = new();
    private readonly LabelControl _lblStatusMessage = new();

    // Tabbed Grids
    private readonly XtraTabControl _tabControl = new();
    private readonly XtraTabPage _tabPrinters = new();
    private readonly XtraTabPage _tabQuarantine = new();

    private readonly GridControl _gridPrinters = new();
    private readonly GridView _viewPrinters = new();
    private readonly List<PrinterHealthDisplayItem> _printersData = new();

    private readonly GridControl _gridQuarantine = new();
    private readonly GridView _viewQuarantine = new();
    private readonly List<QuarantinedJobDisplayItem> _quarantineData = new();

    /// <summary>Designer parameterless constructor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public PrinterHardwareHealthControl() : this(null, null, null, null)
    {
    }

    /// <summary>Constructs the peripheral health control with injected coordinator services.</summary>
    public PrinterHardwareHealthControl(
        PrinterManagementService? printerService,
        IPrintJobQuarantineStore? quarantineStore = null,
        ICircuitBreakerRegistry? circuitBreakerRegistry = null,
        ILogger<PrinterHardwareHealthControl>? logger = null)
    {
        _printerService = printerService;
        _quarantineStore = quarantineStore;
        _circuitBreakerRegistry = circuitBreakerRegistry;
        _logger = logger;

        InitializeLayout();

        if (DesignModeHelper.IsInDesignMode)
        {
            return;
        }

        Load += async (s, e) => await RefreshAllDataAsync();
        WireEvents();
    }

    private void InitializeLayout()
    {
        SuspendLayout();
        Dock = DockStyle.Fill;

        var pad = DesktopDpi.Scale(8, this);
        var cardHeight = DesktopDpi.Scale(68, this);
        var tbHeight = DesktopDpi.Scale(42, this);

        // 1. Top Summary Panel
        _pnlSummary.Dock = DockStyle.Top;
        _pnlSummary.Height = cardHeight;
        _pnlSummary.Padding = new Padding(pad);
        _pnlSummary.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

        var summaryTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1
        };
        summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
        summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));

        ConfigureCardLabel(_lblHealthBanner, "PERIPHERAL HEALTH: CHECKING", Color.DarkSlateGray);
        ConfigureCardLabel(_lblPrinterCountCard, "PRINTERS\n0 Configured", Color.FromArgb(40, 80, 130));
        ConfigureCardLabel(_lblSpoolerQueueCard, "SPOOLER QUEUE\n0 Pending Jobs", Color.FromArgb(40, 120, 80));
        ConfigureCardLabel(_lblQuarantineCountCard, "QUARANTINED\n0 Jobs", Color.FromArgb(160, 80, 40));
        ConfigureCardLabel(_lblCircuitBreakersCard, "CIRCUIT BREAKERS\nAll Closed", Color.FromArgb(70, 70, 90));

        summaryTable.Controls.Add(_lblHealthBanner, 0, 0);
        summaryTable.Controls.Add(_lblPrinterCountCard, 1, 0);
        summaryTable.Controls.Add(_lblSpoolerQueueCard, 2, 0);
        summaryTable.Controls.Add(_lblQuarantineCountCard, 3, 0);
        summaryTable.Controls.Add(_lblCircuitBreakersCard, 4, 0);
        _pnlSummary.Controls.Add(summaryTable);

        // 2. Toolbar Panel
        _pnlToolbar.Dock = DockStyle.Top;
        _pnlToolbar.Height = tbHeight;
        _pnlToolbar.Padding = new Padding(pad, 4, pad, 4);

        var tbFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false
        };

        _btnRefresh.Text = "Refresh Hardware";
        _btnRefresh.Width = DesktopDpi.Scale(130, this);
        _btnRefresh.Height = DesktopDpi.Scale(30, this);

        _btnTestPrint.Text = "Test Print Selected";
        _btnTestPrint.Width = DesktopDpi.Scale(140, this);
        _btnTestPrint.Height = DesktopDpi.Scale(30, this);

        _btnRetryAllQuarantined.Text = "Re-spool All Quarantined";
        _btnRetryAllQuarantined.Width = DesktopDpi.Scale(160, this);
        _btnRetryAllQuarantined.Height = DesktopDpi.Scale(30, this);

        _btnRetrySelectedJob.Text = "Re-spool Selected";
        _btnRetrySelectedJob.Width = DesktopDpi.Scale(130, this);
        _btnRetrySelectedJob.Height = DesktopDpi.Scale(30, this);

        _btnDeleteQuarantinedJob.Text = "Delete Quarantined";
        _btnDeleteQuarantinedJob.Width = DesktopDpi.Scale(130, this);
        _btnDeleteQuarantinedJob.Height = DesktopDpi.Scale(30, this);

        _btnResetBreakers.Text = "Reset Circuit Breakers";
        _btnResetBreakers.Width = DesktopDpi.Scale(150, this);
        _btnResetBreakers.Height = DesktopDpi.Scale(30, this);

        tbFlow.Controls.Add(_btnRefresh);
        tbFlow.Controls.Add(_btnTestPrint);
        tbFlow.Controls.Add(_btnRetryAllQuarantined);
        tbFlow.Controls.Add(_btnRetrySelectedJob);
        tbFlow.Controls.Add(_btnDeleteQuarantinedJob);
        tbFlow.Controls.Add(_btnResetBreakers);

        _lblStatusMessage.Dock = DockStyle.Right;
        _lblStatusMessage.AutoSizeMode = LabelAutoSizeMode.None;
        _lblStatusMessage.Width = DesktopDpi.Scale(260, this);
        _lblStatusMessage.Text = "Ready";
        _lblStatusMessage.Appearance.TextOptions.HAlignment = HorzAlignment.Far;
        _lblStatusMessage.Appearance.TextOptions.VAlignment = VertAlignment.Center;

        _pnlToolbar.Controls.Add(_lblStatusMessage);
        _pnlToolbar.Controls.Add(tbFlow);

        // 3. Tab Control & Grids
        _tabControl.Dock = DockStyle.Fill;
        _tabPrinters.Text = "Hardware & Windows Spoolers";
        _tabQuarantine.Text = "Quarantined Print Jobs Buffer";

        ConfigurePrintersGrid();
        ConfigureQuarantineGrid();

        _tabPrinters.Controls.Add(_gridPrinters);
        _tabQuarantine.Controls.Add(_gridQuarantine);
        _tabControl.TabPages.Add(_tabPrinters);
        _tabControl.TabPages.Add(_tabQuarantine);

        Controls.Add(_tabControl);
        Controls.Add(_pnlToolbar);
        Controls.Add(_pnlSummary);

        ResumeLayout(false);
    }

    private static void ConfigureCardLabel(LabelControl lbl, string text, Color backColor)
    {
        lbl.Dock = DockStyle.Fill;
        lbl.Text = text;
        lbl.Appearance.BackColor = backColor;
        lbl.Appearance.ForeColor = Color.White;
        lbl.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lbl.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
        lbl.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        lbl.Appearance.TextOptions.WordWrap = WordWrap.Wrap;
        lbl.AutoSizeMode = LabelAutoSizeMode.None;
        lbl.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
    }

    private void ConfigurePrintersGrid()
    {
        _gridPrinters.Dock = DockStyle.Fill;
        _gridPrinters.MainView = _viewPrinters;
        _gridPrinters.ViewCollection.Add(_viewPrinters);
        _gridPrinters.DataSource = _printersData;

        _viewPrinters.OptionsBehavior.Editable = false;
        _viewPrinters.OptionsBehavior.ReadOnly = true;
        _viewPrinters.OptionsSelection.EnableAppearanceFocusedCell = false;
        _viewPrinters.OptionsView.ShowGroupPanel = false;
        _viewPrinters.OptionsView.ColumnAutoWidth = true;
        _viewPrinters.RowHeight = DesktopDpi.Scale(28, this);
        _viewPrinters.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        AddColumn(_viewPrinters, nameof(PrinterHealthDisplayItem.ProfileName), "Logical Profile", 140);
        AddColumn(_viewPrinters, nameof(PrinterHealthDisplayItem.SystemPrinterName), "Windows Queue Name", 160);
        AddColumn(_viewPrinters, nameof(PrinterHealthDisplayItem.Role), "Role", 80);
        AddColumn(_viewPrinters, nameof(PrinterHealthDisplayItem.HardwareCondition), "Hardware Status", 150);
        AddColumn(_viewPrinters, nameof(PrinterHealthDisplayItem.SpoolerQueueDepth), "Spooler Depth", 90);
        AddColumn(_viewPrinters, nameof(PrinterHealthDisplayItem.CircuitBreakerState), "Circuit Breaker", 100);
        AddColumn(_viewPrinters, nameof(PrinterHealthDisplayItem.FailureCount), "Trip Count", 80);
        AddColumn(_viewPrinters, nameof(PrinterHealthDisplayItem.LastChecked), "Last Polled", 120);

        _viewPrinters.RowStyle += (s, e) =>
        {
            if (e.RowHandle >= 0 && _viewPrinters.GetRow(e.RowHandle) is PrinterHealthDisplayItem item)
            {
                if (!item.IsReady || string.Equals(item.CircuitBreakerState, "Open", StringComparison.OrdinalIgnoreCase))
                {
                    e.Appearance.BackColor = Color.FromArgb(255, 235, 235);
                    e.Appearance.ForeColor = Color.DarkRed;
                }
                else if (string.Equals(item.CircuitBreakerState, "HalfOpen", StringComparison.OrdinalIgnoreCase) || item.SpoolerQueueDepth > 5)
                {
                    e.Appearance.BackColor = Color.FromArgb(255, 250, 230);
                    e.Appearance.ForeColor = Color.DarkOrange;
                }
            }
        };
    }

    private void ConfigureQuarantineGrid()
    {
        _gridQuarantine.Dock = DockStyle.Fill;
        _gridQuarantine.MainView = _viewQuarantine;
        _gridQuarantine.ViewCollection.Add(_viewQuarantine);
        _gridQuarantine.DataSource = _quarantineData;

        _viewQuarantine.OptionsBehavior.Editable = false;
        _viewQuarantine.OptionsBehavior.ReadOnly = true;
        _viewQuarantine.OptionsSelection.EnableAppearanceFocusedCell = false;
        _viewQuarantine.OptionsView.ShowGroupPanel = false;
        _viewQuarantine.OptionsView.ColumnAutoWidth = true;
        _viewQuarantine.RowHeight = DesktopDpi.Scale(28, this);
        _viewQuarantine.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        AddColumn(_viewQuarantine, nameof(QuarantinedJobDisplayItem.JobId), "Job ID", 110);
        AddColumn(_viewQuarantine, nameof(QuarantinedJobDisplayItem.DocumentType), "Type", 110);
        AddColumn(_viewQuarantine, nameof(QuarantinedJobDisplayItem.TargetPrinter), "Target Printer", 140);
        AddColumn(_viewQuarantine, nameof(QuarantinedJobDisplayItem.QuarantinedAt), "Quarantined At", 130);
        AddColumn(_viewQuarantine, nameof(QuarantinedJobDisplayItem.FailureReason), "Quarantine Reason", 220);
        AddColumn(_viewQuarantine, nameof(QuarantinedJobDisplayItem.RetryCount), "Retries", 60);
        AddColumn(_viewQuarantine, nameof(QuarantinedJobDisplayItem.LastRetryError), "Last Retry Error", 180);
    }

    private static void AddColumn(GridView view, string fieldName, string caption, int width)
    {
        var col = new GridColumn
        {
            FieldName = fieldName,
            Caption = caption,
            Visible = true,
            Width = width
        };
        col.AppearanceHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        view.Columns.Add(col);
    }

    private void WireEvents()
    {
        _btnRefresh.Click += async (s, e) => await RefreshAllDataAsync();
        _btnTestPrint.Click += async (s, e) => await ExecuteSelectedTestPrintAsync();
        _btnRetryAllQuarantined.Click += async (s, e) => await ReplayAllQuarantinedAsync();
        _btnRetrySelectedJob.Click += async (s, e) => await ReplaySelectedQuarantinedAsync();
        _btnDeleteQuarantinedJob.Click += async (s, e) => await DeleteSelectedQuarantinedAsync();
        _btnResetBreakers.Click += async (s, e) => await ResetSelectedCircuitBreakerAsync();
    }

    /// <summary>Refreshes live hardware health, spooler queue depths, and quarantined jobs.</summary>
    public async Task RefreshAllDataAsync()
    {
        if (_printerService == null)
        {
            _lblStatusMessage.Text = "Printer service not configured.";
            return;
        }

        try
        {
            _lblStatusMessage.Text = "Polling peripheral health...";
            var config = await _printerService.GetConfigurationAsync();
            var snapshots = await _printerService.GetAllPrintersHealthAsync();

            _printersData.Clear();
            int totalSpoolerJobs = 0;
            int faultCount = 0;
            int openBreakersCount = 0;

            foreach (var profile in config.Profiles)
            {
                var snap = snapshots.FirstOrDefault(s => s.ProfileId == profile.Id || s.PrinterName.Equals(profile.SystemPrinterName, StringComparison.OrdinalIgnoreCase))
                           ?? PrinterHealthSnapshot.Faulted(profile.SystemPrinterName, PrinterHardwareCondition.Offline, "Unreachable", profile.Id);

                var breaker = _printerService.GetCircuitBreaker(profile.SystemPrinterName);
                var breakerState = breaker.State.ToString();
                if (breaker.State == CircuitBreakerState.Open)
                {
                    openBreakersCount++;
                }

                if (!snap.IsReady)
                {
                    faultCount++;
                }

                totalSpoolerJobs += snap.SpoolerQueueDepth;

                _printersData.Add(new PrinterHealthDisplayItem
                {
                    ProfileId = profile.Id,
                    ProfileName = profile.ProfileName,
                    SystemPrinterName = profile.SystemPrinterName,
                    Role = profile.Role.ToString(),
                    HardwareCondition = snap.IsReady ? "Ready" : $"{snap.Condition}: {snap.StatusMessage}",
                    IsReady = snap.IsReady,
                    SpoolerQueueDepth = snap.SpoolerQueueDepth,
                    CircuitBreakerState = breakerState,
                    FailureCount = breaker.FailureCount,
                    LastChecked = BusinessDateTimeFormatter.Format(snap.CheckedAtUtc)
                });
            }

            _viewPrinters.RefreshData();

            // Load Quarantined Jobs
            var quarantined = await _printerService.GetQuarantinedJobsAsync();
            _quarantineData.Clear();
            foreach (var q in quarantined)
            {
                _quarantineData.Add(new QuarantinedJobDisplayItem
                {
                    JobId = q.JobId,
                    DocumentType = q.DocumentType,
                    TargetPrinter = q.TargetSystemPrinterName,
                    QuarantinedAt = BusinessDateTimeFormatter.Format(q.QuarantinedAtUtc),
                    FailureReason = q.FailureReason,
                    RetryCount = q.RetryCount,
                    LastRetryError = q.LastRetryError ?? string.Empty
                });
            }
            _viewQuarantine.RefreshData();

            // Update Summary KPI Cards
            _lblPrinterCountCard.Text = $"PRINTERS\n{config.Profiles.Count} Configured";
            _lblSpoolerQueueCard.Text = $"SPOOLER QUEUE\n{totalSpoolerJobs} Pending Jobs";
            _lblQuarantineCountCard.Text = $"QUARANTINED\n{quarantined.Count} Buffered Jobs";
            _lblCircuitBreakersCard.Text = openBreakersCount == 0 ? "CIRCUIT BREAKERS\nAll Closed" : $"CIRCUIT BREAKERS\n{openBreakersCount} Tripped (Open)";

            if (faultCount == 0 && openBreakersCount == 0 && quarantined.Count == 0)
            {
                _lblHealthBanner.Text = "PERIPHERAL HEALTH\nALL PRINTERS READY";
                _lblHealthBanner.Appearance.BackColor = Color.FromArgb(40, 140, 60);
            }
            else
            {
                _lblHealthBanner.Text = $"PERIPHERAL HEALTH\nATTENTION: {faultCount} Fault(s), {quarantined.Count} Quarantined";
                _lblHealthBanner.Appearance.BackColor = Color.FromArgb(180, 50, 40);
            }

            _tabQuarantine.Text = $"Quarantined Print Jobs Buffer ({quarantined.Count})";
            _lblStatusMessage.Text = $"Updated at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed refreshing printer health monitor data");
            _lblStatusMessage.Text = $"Error: {ex.Message}";
        }
    }

    private async Task ExecuteSelectedTestPrintAsync()
    {
        if (_printerService == null) return;

        var focused = _viewPrinters.GetFocusedRow() as PrinterHealthDisplayItem;
        if (focused == null)
        {
            XtraMessageBox.Show("Please select a printer profile from the grid first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Strict authorization rule: Explicit operator confirmation before actuating hardware
        var prompt = XtraMessageBox.Show(
            $"Authorize physical test print to queue '{focused.SystemPrinterName}' for profile '{focused.ProfileName}'?",
            "Authorize Peripheral Test Print",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (prompt != DialogResult.Yes)
        {
            _lblStatusMessage.Text = "Test print cancelled.";
            return;
        }

        _lblStatusMessage.Text = $"Executing test print to {focused.SystemPrinterName}...";
        var result = await _printerService.ExecuteTestPrintAsync(focused.ProfileId, "Administrator", authorizedExplicitly: true);

        if (result.Success)
        {
            XtraMessageBox.Show(
                $"Test print accepted by print spooler for '{focused.SystemPrinterName}'. Please verify paper output on the physical device.",
                "Print Dispatched",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            XtraMessageBox.Show(
                $"Failed to dispatch test print to '{focused.SystemPrinterName}':\n\n{result.ErrorMessage}",
                "Test Print Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        await RefreshAllDataAsync();
    }

    private async Task ReplayAllQuarantinedAsync()
    {
        if (_printerService == null) return;

        if (_quarantineData.Count == 0)
        {
            XtraMessageBox.Show("No quarantined print jobs to recover.", "Queue Empty", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _lblStatusMessage.Text = "Re-spooling all quarantined jobs...";
        var summary = await _printerService.ReplayAllQuarantinedJobsAsync();

        XtraMessageBox.Show(
            $"Re-spooling Complete:\n\n• Successfully recovered: {summary.SuccessfullyRecovered}\n• Still failing / offline: {summary.StillFailing}\n• Total processed: {summary.TotalAttempted}",
            "Recovery Summary",
            MessageBoxButtons.OK,
            summary.StillFailing > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

        await RefreshAllDataAsync();
    }

    private async Task ReplaySelectedQuarantinedAsync()
    {
        if (_printerService == null) return;

        var focused = _viewQuarantine.GetFocusedRow() as QuarantinedJobDisplayItem;
        if (focused == null)
        {
            XtraMessageBox.Show("Please select a quarantined job to retry.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _lblStatusMessage.Text = $"Retrying job {focused.JobId}...";
        var result = await _printerService.ReplayQuarantinedJobAsync(focused.JobId);

        if (result.Success)
        {
            XtraMessageBox.Show($"Successfully re-spooled quarantined job {focused.JobId} to '{focused.TargetPrinter}'.",
                "Recovery Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            XtraMessageBox.Show($"Retry failed for job {focused.JobId}:\n\n{result.ErrorMessage}",
                "Retry Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        await RefreshAllDataAsync();
    }

    private async Task DeleteSelectedQuarantinedAsync()
    {
        if (_printerService == null) return;

        var focused = _viewQuarantine.GetFocusedRow() as QuarantinedJobDisplayItem;
        if (focused == null)
        {
            XtraMessageBox.Show("Please select a quarantined job to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = XtraMessageBox.Show(
            $"Are you sure you want to permanently delete quarantined job {focused.JobId} ({focused.DocumentType}) without printing?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            await _printerService.DeleteQuarantinedJobAsync(focused.JobId);
            await RefreshAllDataAsync();
        }
    }

    private async Task ResetSelectedCircuitBreakerAsync()
    {
        if (_printerService == null) return;

        var focused = _viewPrinters.GetFocusedRow() as PrinterHealthDisplayItem;
        if (focused == null)
        {
            XtraMessageBox.Show("Please select a printer profile from the grid to reset its circuit breaker.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _printerService.ResetCircuitBreaker(focused.SystemPrinterName);
        _lblStatusMessage.Text = $"Reset circuit breaker for {focused.SystemPrinterName}.";
        await RefreshAllDataAsync();
    }
}
