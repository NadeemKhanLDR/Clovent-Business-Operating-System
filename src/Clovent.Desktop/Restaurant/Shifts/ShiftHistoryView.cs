using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.Terminals.Queries;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Application.Shifts.Queries;
using Clovent.Restaurant.Shifts;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Shifts;

/// <summary>
/// Shift History &amp; Management View: DevExpress grid view for listing, searching, auditing,
/// opening, performing cash movements on, and closing cash register shifts.
/// Features a QuickBooks-style report period selector and responsive DPI layout.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class ShiftHistoryView : XtraUserControl
{
    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly ILogger<ShiftHistoryView> _logger;

    private ComboBoxEdit _cboPeriod = null!;
    private DateEdit _dtFrom = null!;
    private DateEdit _dtTo = null!;
    private ComboBoxEdit _cboStatus = null!;
    private SimpleButton _btnSearch = null!;
    private SimpleButton _btnClear = null!;

    private SimpleButton _btnOpenShift = null!;
    private SimpleButton _btnCashMovement = null!;
    private SimpleButton _btnCloseShift = null!;
    private SimpleButton _btnViewDetails = null!;

    private GridControl _gridControl = null!;
    private GridView _gridView = null!;

    private FlowLayoutPanel _filterPanel = null!;
    private FlowLayoutPanel _actionBar = null!;

    private IReadOnlyList<ShiftDto> _shifts = [];
    private bool _isUpdatingPeriod;

    /// <summary>Design-time constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ShiftHistoryView()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;
        _logger = null!;
        BuildUi();
    }

    /// <summary>Constructs ShiftHistoryView with DI services.</summary>
    public ShiftHistoryView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession, ILogger<ShiftHistoryView> logger)
    {
        InitializeComponent();

        if (DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _featurePolicy = null!;
            _currentSession = null!;
            _logger = null!;
            BuildUi();
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;
        _logger = logger;

        BuildUi();
        Load += ShiftHistoryView_Load;
        Resize += (_, _) => ScaleLayoutAtRuntime();
    }

    private void InitializeComponent() { }

    private void BuildUi()
    {
        Dock = DockStyle.Fill;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16, 12, 16, 12)
        };

        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header + Filters
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Action Bar

        // ---- Top Header & Filter Container ----
        var topContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 10)
        };
        topContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        topContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Title and Subtitle
        var titlePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 8)
        };
        titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new LabelControl
        {
            Text = "SHIFT HISTORY",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 2)
        };
        var subtitleLabel = new LabelControl
        {
            Text = "Review cashier shifts, balances and shift activity.",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = Color.Gray,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 4)
        };
        titlePanel.Controls.Add(titleLabel, 0, 0);
        titlePanel.Controls.Add(subtitleLabel, 0, 1);

        // Filter Bar
        _filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 4, 0, 4),
            Margin = new Padding(0)
        };

        void AddFilterControl(Control c, int rightMargin = 12)
        {
            c.Anchor = AnchorStyles.Left;
            c.Margin = new Padding(0, 4, rightMargin, 4);
            _filterPanel.Controls.Add(c);
        }

        LabelControl CreateFilterLabel(string text) => new()
        {
            Text = text,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            AutoSize = true,
            Padding = new Padding(0, 6, 4, 0)
        };

        // 1. Period
        AddFilterControl(CreateFilterLabel("Period:"), 4);
        _cboPeriod = new ComboBoxEdit
        {
            Properties = { TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor },
            Font = new Font("Segoe UI", 9.5F)
        };
        foreach (var (_, name) in ReportPeriodCalculator.GetAllOptions())
        {
            _cboPeriod.Properties.Items.Add(name);
        }
        _cboPeriod.SelectedIndexChanged += CboPeriod_SelectedIndexChanged;
        AddFilterControl(_cboPeriod, 16);

        // 2. From
        AddFilterControl(CreateFilterLabel("From:"), 4);
        _dtFrom = new DateEdit
        {
            Font = new Font("Segoe UI", 9.5F)
        };
        _dtFrom.EditValueChanged += DateEdits_EditValueChanged;
        AddFilterControl(_dtFrom, 16);

        // 3. To
        AddFilterControl(CreateFilterLabel("To:"), 4);
        _dtTo = new DateEdit
        {
            Font = new Font("Segoe UI", 9.5F)
        };
        _dtTo.EditValueChanged += DateEdits_EditValueChanged;
        AddFilterControl(_dtTo, 16);

        // 4. Status
        AddFilterControl(CreateFilterLabel("Status:"), 4);
        _cboStatus = new ComboBoxEdit
        {
            Properties = { TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor },
            Font = new Font("Segoe UI", 9.5F)
        };
        _cboStatus.Properties.Items.AddRange(new[] { "All", "Open", "Closed", "Cancelled" });
        _cboStatus.SelectedIndex = 0;
        AddFilterControl(_cboStatus, 16);

        // 5. Search
        _btnSearch = new SimpleButton
        {
            Text = "Search",
            Appearance = { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) },
            Cursor = Cursors.Hand
        };
        _btnSearch.Click += BtnSearch_Click;
        AddFilterControl(_btnSearch, 8);

        // 6. Clear Filters
        _btnClear = new SimpleButton
        {
            Text = "Clear Filters",
            Appearance = { Font = new Font("Segoe UI", 9.5F) },
            Cursor = Cursors.Hand
        };
        _btnClear.Click += BtnClear_Click;
        AddFilterControl(_btnClear, 0);

        topContainer.Controls.Add(titlePanel, 0, 0);
        topContainer.Controls.Add(_filterPanel, 0, 1);

        // Set default period to This Month
        _cboPeriod.SelectedItem = "This Month";

        // ---- Grid ----
        _gridControl = new GridControl { Dock = DockStyle.Fill };
        _gridView = new GridView(_gridControl)
        {
            OptionsBehavior = { Editable = false },
            OptionsView = { ShowGroupPanel = false, ShowAutoFilterRow = true, ShowFooter = true },
            RowHeight = 30,
            ColumnPanelRowHeight = 32
        };
        _gridControl.MainView = _gridView;
        _gridView.DoubleClick += GridView_DoubleClick;
        _gridView.CustomColumnDisplayText += GridView_CustomColumnDisplayText;

        // Explicit business-facing columns — hides raw GUID IDs from manager view
        AddShiftColumn("ShiftNumber",    "Shift #",        60,  DevExpress.Utils.HorzAlignment.Center);
        AddShiftColumn("CashierName",    "Cashier",        130, DevExpress.Utils.HorzAlignment.Near);
        AddShiftColumn("OpenedAtUtc",    "Opened",         155, DevExpress.Utils.HorzAlignment.Near);
        AddShiftColumn("ClosedAtUtc",    "Closed",         155, DevExpress.Utils.HorzAlignment.Near);
        AddShiftColumn("Status",         "Status",         80,  DevExpress.Utils.HorzAlignment.Center);
        AddShiftColumn("StartingCash",   "Starting Cash",  110, DevExpress.Utils.HorzAlignment.Far);
        AddShiftColumn("ExpectedCash",   "Expected Cash",  110, DevExpress.Utils.HorzAlignment.Far);
        AddShiftColumn("CountedCash",    "Counted Cash",   110, DevExpress.Utils.HorzAlignment.Far);
        AddShiftColumn("CashVariance",   "Variance",       100, DevExpress.Utils.HorzAlignment.Far);
        AddShiftColumn("VarianceReason", "Reason",         160, DevExpress.Utils.HorzAlignment.Near);
        AddShiftColumn("Notes",          "Notes",          200, DevExpress.Utils.HorzAlignment.Near);

        // Empty state overlay
        _gridView.CustomDrawEmptyForeground += (sender, e) =>
        {
            if (_shifts.Count == 0)
            {
                const string message = "No shifts found for the selected period.";
                using var font = new Font("Segoe UI", 11F, FontStyle.Regular);
                using var brush = new SolidBrush(Color.FromArgb(100, 116, 139));
                using var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(message, font, brush, e.Bounds, format);
            }
        };

        // ---- Bottom Action Bar ----
        _actionBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 10, 0, 0),
            Margin = new Padding(0)
        };

        _btnOpenShift = new SimpleButton
        {
            Text = "Open Shift",
            Appearance = { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) },
            Cursor = Cursors.Hand
        };
        _btnOpenShift.Click += BtnOpenShift_Click;

        _btnCashMovement = new SimpleButton
        {
            Text = "Cash In / Cash Out",
            Appearance = { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) },
            Cursor = Cursors.Hand
        };
        _btnCashMovement.Click += BtnCashMovement_Click;

        _btnCloseShift = new SimpleButton
        {
            Text = "Close Shift",
            Appearance = { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) },
            Cursor = Cursors.Hand
        };
        _btnCloseShift.Click += BtnCloseShift_Click;

        _btnViewDetails = new SimpleButton
        {
            Text = "View Details",
            Appearance = { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) },
            Cursor = Cursors.Hand
        };
        _btnViewDetails.Click += BtnViewDetails_Click;

        void AddActionButton(SimpleButton btn)
        {
            btn.AutoSize = true;
            btn.Padding = new Padding(14, 6, 14, 6);
            btn.Margin = new Padding(0, 0, 10, 0);
            _actionBar.Controls.Add(btn);
        }

        AddActionButton(_btnOpenShift);
        AddActionButton(_btnCashMovement);
        AddActionButton(_btnCloseShift);
        AddActionButton(_btnViewDetails);

        mainLayout.Controls.Add(topContainer, 0, 0);
        mainLayout.Controls.Add(_gridControl, 0, 1);
        mainLayout.Controls.Add(_actionBar, 0, 2);

        Controls.Add(mainLayout);

        ScaleLayoutAtRuntime();
        AppearanceManager.Apply(this, "Restaurant", nameof(ShiftHistoryView));
    }

    private void AddShiftColumn(string fieldName, string caption, int width, DevExpress.Utils.HorzAlignment alignment = DevExpress.Utils.HorzAlignment.Near)
    {
        var col = _gridView.Columns.AddVisible(fieldName, caption);
        col.Width = width;
        col.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        col.AppearanceHeader.Options.UseFont = true;
        col.AppearanceCell.TextOptions.HAlignment = alignment;
        col.AppearanceCell.Options.UseTextOptions = true;
    }

    private void GridView_CustomColumnDisplayText(object? sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Value == null || e.Value == DBNull.Value) return;

        switch (e.Column.FieldName)
        {
            case "OpenedAtUtc" when e.Value is DateTimeOffset dtoOpened:
                e.DisplayText = Forms.Base.DateTimeDisplay.Format(dtoOpened);
                break;
            case "ClosedAtUtc" when e.Value is DateTimeOffset dtoClosed:
                e.DisplayText = Forms.Base.DateTimeDisplay.Format(dtoClosed);
                break;
            case "CashierName" when e.Value is string cashier:
                e.DisplayText = Forms.Base.UserDisplayNameHelper.FormatCashierName(cashier);
                break;
            case "StartingCash" or "ExpectedCash" or "CountedCash" or "CashVariance":
                try { e.DisplayText = Forms.Base.CurrencyDisplay.FormatPlain(Convert.ToDecimal(e.Value)); }
                catch { }
                break;
        }
    }

    private void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        int btnH = DesktopDpi.Scale(36, this);
        int editH = DesktopDpi.Scale(30, this);

        _cboPeriod.Size = new Size(DesktopDpi.Scale(130, this), editH);
        _dtFrom.Size = new Size(DesktopDpi.Scale(130, this), editH);
        _dtTo.Size = new Size(DesktopDpi.Scale(130, this), editH);
        _cboStatus.Size = new Size(DesktopDpi.Scale(110, this), editH);
        _btnSearch.MinimumSize = new Size(DesktopDpi.Scale(90, this), btnH);
        _btnClear.MinimumSize = new Size(DesktopDpi.Scale(100, this), btnH);

        _btnOpenShift.MinimumSize = new Size(DesktopDpi.Scale(120, this), btnH);
        _btnCashMovement.MinimumSize = new Size(DesktopDpi.Scale(150, this), btnH);
        _btnCloseShift.MinimumSize = new Size(DesktopDpi.Scale(120, this), btnH);
        _btnViewDetails.MinimumSize = new Size(DesktopDpi.Scale(120, this), btnH);
    }

    private void CboPeriod_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingPeriod) return;

        var periodName = _cboPeriod.SelectedItem?.ToString();
        var period = ReportPeriodCalculator.ParseDisplayName(periodName);
        if (period == ReportPeriod.Custom) return;

        _isUpdatingPeriod = true;
        try
        {
            var range = ReportPeriodCalculator.CalculateRange(period, DateOnly.FromDateTime(DateTime.Today));
            _dtFrom.EditValue = range.From.ToDateTime(TimeOnly.MinValue);
            _dtTo.EditValue = range.To.ToDateTime(TimeOnly.MinValue);
        }
        finally
        {
            _isUpdatingPeriod = false;
        }
    }

    private void DateEdits_EditValueChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingPeriod) return;

        // When dates are manually modified, switch the period dropdown to Custom
        if (_cboPeriod.SelectedItem?.ToString() != "Custom")
        {
            _isUpdatingPeriod = true;
            try
            {
                _cboPeriod.SelectedItem = "Custom";
            }
            finally
            {
                _isUpdatingPeriod = false;
            }
        }
    }

    private async void BtnClear_Click(object? sender, EventArgs e)
    {
        _cboStatus.SelectedIndex = 0;
        _cboPeriod.SelectedItem = "This Month";
        await LoadShiftsAsync();
    }

    private async void ShiftHistoryView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode) return;
        ScaleLayoutAtRuntime();
        await LoadShiftsAsync();
    }

    private async void BtnSearch_Click(object? sender, EventArgs e)
    {
        await LoadShiftsAsync();
    }

    private async System.Threading.Tasks.Task LoadShiftsAsync()
    {
        try
        {
            DateTimeOffset? fromDate = null;
            DateTimeOffset? toDate = null;

            if (_dtFrom.EditValue is DateTime dFrom && _dtTo.EditValue is DateTime dTo)
            {
                var range = new DateRange(DateOnly.FromDateTime(dFrom), DateOnly.FromDateTime(dTo));
                if (!range.IsValid)
                {
                    XtraMessageBox.Show(this, "'To' date cannot be before 'From' date.", "Invalid Date Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                fromDate = range.StartOfFromUtc;
                toDate = range.EndOfToUtcInclusive;
            }
            else if (_dtFrom.EditValue is DateTime dFromOnly)
            {
                fromDate = new DateTimeOffset(dFromOnly.Date, TimeSpan.Zero);
            }
            else if (_dtTo.EditValue is DateTime dToOnly)
            {
                toDate = new DateTimeOffset(dToOnly.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero);
            }

            ShiftStatus? status = _cboStatus.Text switch
            {
                "Open" => ShiftStatus.Open,
                "Closed" => ShiftStatus.Closed,
                "Cancelled" => ShiftStatus.Cancelled,
                _ => null
            };

            var query = new ListShiftsQuery(Status: status, FromDateUtc: fromDate, ToDateUtc: toDate);
            _shifts = await _mediator.Send(query);
            _gridControl.DataSource = _shifts.Select(ShiftGridRow.From).ToList();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to load shifts: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnOpenShift_Click(object? sender, EventArgs e)
    {
        var branchId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var terminalId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var warehouseId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        using var dialog = new OpenShiftDialog(_mediator, _currentSession, branchId, warehouseId, terminalId);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadShiftsAsync();
        }
    }

    private ShiftDto? GetSelectedShift()
    {
        if (_gridView.GetFocusedRow() is not ShiftGridRow row) return null;
        return _shifts.FirstOrDefault(s => s.ShiftId == row.OriginalShiftId);
    }

    private void BtnCashMovement_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedShift();
        if (selected == null)
        {
            XtraMessageBox.Show(this, "Please select a shift first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (selected.Status != "Open")
        {
            XtraMessageBox.Show(this, "Cash movements can only be recorded on an OPEN shift.", "Invalid Action", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new CashMovementDialog(_mediator, _currentSession, selected.ShiftId);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadShiftsAsync();
        }
    }

    private void BtnCloseShift_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedShift();
        if (selected == null)
        {
            XtraMessageBox.Show(this, "Please select a shift first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (selected.Status != "Open")
        {
            XtraMessageBox.Show(this, "Only an OPEN shift can be closed.", "Invalid Action", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new CloseShiftDialog(_mediator, selected.ShiftId);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadShiftsAsync();
        }
    }

    private void BtnViewDetails_Click(object? sender, EventArgs e)
    {
        ShowSelectedDetail();
    }

    private void GridView_DoubleClick(object? sender, EventArgs e)
    {
        ShowSelectedDetail();
    }

    private void ShowSelectedDetail()
    {
        var selected = GetSelectedShift();
        if (selected == null) return;

        using var dialog = new ShiftDetailDialog(_mediator, selected.ShiftId);
        dialog.ShowDialog(this);
    }

    private sealed record ShiftGridRow(
        int ShiftNumber,
        string CashierName,
        DateTimeOffset OpenedAtUtc,
        DateTimeOffset? ClosedAtUtc,
        string Status,
        decimal StartingCash,
        decimal ExpectedCash,
        decimal CountedCash,
        decimal CashVariance,
        string? VarianceReason,
        string? Notes)
    {
        internal Guid OriginalShiftId { get; init; }

        internal static ShiftGridRow From(ShiftDto dto) => new(
            dto.ShiftNumber,
            dto.CashierName,
            dto.OpenedAtUtc,
            dto.ClosedAtUtc,
            dto.Status,
            dto.StartingCash,
            dto.ExpectedCash,
            dto.CountedCash,
            dto.CashVariance,
            dto.VarianceReason,
            dto.Notes)
        {
            OriginalShiftId = dto.ShiftId
        };
    }
}
