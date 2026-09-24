using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.DayClose.Commands;
using Clovent.Restaurant.Application.DayClose.Dtos;
using Clovent.Restaurant.Application.DayClose.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;

namespace Clovent.Desktop.Restaurant.EndOfDay;

/// <summary>
/// Professional manager dialog for reviewing and executing Business Day Close.
/// Strictly enforces that all shifts on the business date must be closed prior to Day Close.
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

    private LabelControl _lblTotalShifts = null!;
    private LabelControl _lblOpenShifts = null!;
    private LabelControl _lblTotalSales = null!;
    private LabelControl _lblCashSales = null!;
    private LabelControl _lblCardSales = null!;
    private LabelControl _lblVariance = null!;

    private GridControl _gridShifts = null!;
    private GridView _viewShifts = null!;

    private MemoEdit _txtNotes = null!;
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
        Text = $"End of Day (Day Close) - {DateTimeDisplay.FormatDate(_businessDate)}";
        DesktopDialogSizing.Apply(this, 760, 620, 650, 520, null, false);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(16, this))
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Header
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));  // Alert/Status Banner
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));  // Metrics Summary Cards
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Shifts Grid
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Notes
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));  // Buttons

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
            Text = "Review all shift registers and financial totals before finalizing and locking this business date.",
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
            Appearance = { Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center, VAlignment = DevExpress.Utils.VertAlignment.Center } },
            Text = "Loading business day data..."
        };
        _bannerPanel.Controls.Add(_lblBanner);
        mainPanel.Controls.Add(_bannerPanel, 0, 1);

        // 3. Metrics Summary Cards (2 rows x 3 cols)
        var tlpMetrics = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0, 4, 0, 4)
        };
        tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        tlpMetrics.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        tlpMetrics.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        _lblTotalShifts = CreateMetricCard("Total Shifts", "-", Color.FromArgb(30, 41, 59), tlpMetrics, 0, 0);
        _lblOpenShifts = CreateMetricCard("Open Shifts", "-", Color.FromArgb(220, 38, 38), tlpMetrics, 1, 0);
        _lblTotalSales = CreateMetricCard("Total Net Sales", "-", Color.FromArgb(13, 148, 136), tlpMetrics, 2, 0);

        _lblCashSales = CreateMetricCard("Cash Sales", "-", Color.FromArgb(30, 41, 59), tlpMetrics, 0, 1);
        _lblCardSales = CreateMetricCard("Card/Other Sales", "-", Color.FromArgb(30, 41, 59), tlpMetrics, 1, 1);
        _lblVariance = CreateMetricCard("Shift Cash Variance", "-", Color.FromArgb(30, 41, 59), tlpMetrics, 2, 1);

        mainPanel.Controls.Add(tlpMetrics, 0, 2);

        // 4. Shifts Grid
        _gridShifts = new GridControl { Dock = DockStyle.Fill };
        _viewShifts = new GridView(_gridShifts);
        _gridShifts.MainView = _viewShifts;
        _viewShifts.OptionsBehavior.Editable = false;
        _viewShifts.OptionsView.ShowGroupPanel = false;
        _viewShifts.OptionsView.ShowIndicator = false;
        _viewShifts.RowHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(26, this);
        mainPanel.Controls.Add(_gridShifts, 0, 3);

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
            Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(34, this),
            Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(160, this),
            Appearance = { Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = Color.FromArgb(13, 148, 136), ForeColor = Color.White },
            Cursor = Cursors.Hand,
            Enabled = false
        };
        _btnCloseDay.Click += async (s, e) => await CloseDayAsync();

        _btnCancel = new SimpleButton
        {
            Text = "Cancel",
            Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(34, this),
            Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(90, this),
            DialogResult = DialogResult.Cancel,
            Cursor = Cursors.Hand
        };

        pnlButtons.Controls.Add(_btnCloseDay);
        pnlButtons.Controls.Add(_btnCancel);
        mainPanel.Controls.Add(pnlButtons, 0, 5);

        Controls.Add(mainPanel);
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
            _summary = await _mediator.Send(new GetBusinessDaySummaryQuery(_branchId, _businessDate));
            if (_summary == null)
            {
                _lblBanner.Text = "Failed to load business day summary.";
                _bannerPanel.Appearance.BackColor = Color.FromArgb(254, 242, 242);
                _lblBanner.Appearance.ForeColor = Color.FromArgb(220, 38, 38);
                return;
            }

            // Populate cards
            _lblTotalShifts.Text = _summary.TotalShifts.ToString();
            _lblOpenShifts.Text = _summary.OpenShifts.Count.ToString();
            _lblTotalSales.Text = CurrencyDisplay.Format(_summary.TotalSales);
            _lblCashSales.Text = CurrencyDisplay.Format(_summary.CashSales);
            _lblCardSales.Text = CurrencyDisplay.Format(_summary.CardSales + _summary.OtherSales);
            
            var varText = CurrencyDisplay.Format(_summary.TotalShiftVariance);
            _lblVariance.Text = varText;
            _lblVariance.Appearance.ForeColor = _summary.TotalShiftVariance < 0
                ? Color.FromArgb(220, 38, 38)
                : (_summary.TotalShiftVariance > 0 ? Color.FromArgb(22, 163, 74) : Color.FromArgb(30, 41, 59));

            // Populate grid with all shifts (open + closed)
            var allShifts = _summary.OpenShifts.Concat(_summary.ClosedShifts).OrderBy(s => s.ShiftNumber).ToList();
            _gridShifts.DataSource = allShifts;

            // Configure columns
            if (_viewShifts.Columns.Count > 0)
            {
                if (_viewShifts.Columns["ShiftId"] != null) _viewShifts.Columns["ShiftId"].Visible = false;
                if (_viewShifts.Columns["TerminalId"] != null) _viewShifts.Columns["TerminalId"].Visible = false;
                if (_viewShifts.Columns["StartingCash"] != null) _viewShifts.Columns["StartingCash"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                if (_viewShifts.Columns["StartingCash"] != null) _viewShifts.Columns["StartingCash"].DisplayFormat.FormatString = "n2";
                if (_viewShifts.Columns["ExpectedCash"] != null) _viewShifts.Columns["ExpectedCash"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                if (_viewShifts.Columns["ExpectedCash"] != null) _viewShifts.Columns["ExpectedCash"].DisplayFormat.FormatString = "n2";
                if (_viewShifts.Columns["CountedCash"] != null) _viewShifts.Columns["CountedCash"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                if (_viewShifts.Columns["CountedCash"] != null) _viewShifts.Columns["CountedCash"].DisplayFormat.FormatString = "n2";
                if (_viewShifts.Columns["CashVariance"] != null) _viewShifts.Columns["CashVariance"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                if (_viewShifts.Columns["CashVariance"] != null) _viewShifts.Columns["CashVariance"].DisplayFormat.FormatString = "n2";
            }

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
        catch (Exception ex)
        {
            _bannerPanel.Appearance.BackColor = Color.FromArgb(254, 242, 242);
            _lblBanner.Appearance.ForeColor = Color.FromArgb(220, 38, 38);
            _lblBanner.Text = $"Error loading business day summary: {ex.Message}";
            _btnCloseDay.Enabled = false;
        }
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
            $"Shift Cash Variance: {CurrencyDisplay.Format(_summary.TotalShiftVariance)}\n\n" +
            "This action finalizes and locks the business day.",
            "Confirm Business Day Close",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        _btnCloseDay.Enabled = false;
        try
        {
            var command = new CloseBusinessDayCommand(
                _branchId,
                _businessDate,
                _currentSession.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001"),
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
