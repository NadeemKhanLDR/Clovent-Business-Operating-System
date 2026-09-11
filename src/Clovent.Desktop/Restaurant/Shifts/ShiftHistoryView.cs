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

    private DateEdit _dtFrom = null!;
    private DateEdit _dtTo = null!;
    private ComboBoxEdit _cboStatus = null!;
    private SimpleButton _btnSearch = null!;

    private SimpleButton _btnOpenShift = null!;
    private SimpleButton _btnCashMovement = null!;
    private SimpleButton _btnCloseShift = null!;
    private SimpleButton _btnViewDetails = null!;

    private GridControl _gridControl = null!;
    private GridView _gridView = null!;

    private IReadOnlyList<ShiftDto> _shifts = [];

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
    }

    private void InitializeComponent() { }

    private void BuildUi()
    {
        Dock = DockStyle.Fill;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            RowCount = 3,
            ColumnCount = 1
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f)); // Filter panel
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // Toolbar buttons

        // Top Filter Bar
        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 5)
        };

        filterPanel.Controls.Add(new LabelControl { Text = "From:", AutoSize = false, Size = new Size(45, 28) });
        _dtFrom = new DateEdit { EditValue = DateTime.Today.AddDays(-7), Size = new Size(130, 28) };
        filterPanel.Controls.Add(_dtFrom);

        filterPanel.Controls.Add(new LabelControl { Text = "To:", AutoSize = false, Size = new Size(30, 28) });
        _dtTo = new DateEdit { EditValue = DateTime.Today.AddDays(1), Size = new Size(130, 28) };
        filterPanel.Controls.Add(_dtTo);

        filterPanel.Controls.Add(new LabelControl { Text = "Status:", AutoSize = false, Size = new Size(50, 28) });
        _cboStatus = new ComboBoxEdit
        {
            Size = new Size(120, 28),
            Properties = { TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor }
        };
        _cboStatus.Properties.Items.AddRange(new[] { "All", "Open", "Closed", "Cancelled" });
        _cboStatus.SelectedIndex = 0;
        filterPanel.Controls.Add(_cboStatus);

        _btnSearch = new SimpleButton { Text = "Search", Size = new Size(90, 30) };
        _btnSearch.Click += BtnSearch_Click;
        filterPanel.Controls.Add(_btnSearch);

        // Grid
        _gridControl = new GridControl { Dock = DockStyle.Fill };
        _gridView = new GridView(_gridControl)
        {
            OptionsBehavior = { Editable = false },
            OptionsView = { ShowGroupPanel = false, ShowAutoFilterRow = true, ShowFooter = true }
        };
        _gridControl.MainView = _gridView;
        _gridView.DoubleClick += GridView_DoubleClick;

        // Bottom Action Bar
        var actionBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 0)
        };

        _btnOpenShift = new SimpleButton { Text = "Open Shift", Size = new Size(110, 35) };
        _btnOpenShift.Click += BtnOpenShift_Click;

        _btnCashMovement = new SimpleButton { Text = "Cash In / Out", Size = new Size(120, 35) };
        _btnCashMovement.Click += BtnCashMovement_Click;

        _btnCloseShift = new SimpleButton { Text = "Close Shift", Size = new Size(110, 35) };
        _btnCloseShift.Click += BtnCloseShift_Click;

        _btnViewDetails = new SimpleButton { Text = "View Details", Size = new Size(110, 35) };
        _btnViewDetails.Click += BtnViewDetails_Click;

        actionBar.Controls.Add(_btnOpenShift);
        actionBar.Controls.Add(_btnCashMovement);
        actionBar.Controls.Add(_btnCloseShift);
        actionBar.Controls.Add(_btnViewDetails);

        mainLayout.Controls.Add(filterPanel, 0, 0);
        mainLayout.Controls.Add(_gridControl, 0, 1);
        mainLayout.Controls.Add(actionBar, 0, 2);

        Controls.Add(mainLayout);

        AppearanceManager.Apply(this, "Restaurant", nameof(ShiftHistoryView));
    }

    private async void ShiftHistoryView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode) return;
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
            DateTimeOffset? fromDate = _dtFrom.EditValue is DateTime dFrom ? new DateTimeOffset(dFrom.Date, TimeSpan.Zero) : null;
            DateTimeOffset? toDate = _dtTo.EditValue is DateTime dTo ? new DateTimeOffset(dTo.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero) : null;

            ShiftStatus? status = _cboStatus.Text switch
            {
                "Open" => ShiftStatus.Open,
                "Closed" => ShiftStatus.Closed,
                "Cancelled" => ShiftStatus.Cancelled,
                _ => null
            };

            var query = new ListShiftsQuery(Status: status, FromDateUtc: fromDate, ToDateUtc: toDate);
            _shifts = await _mediator.Send(query);
            _gridControl.DataSource = _shifts;
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
        return _gridView.GetFocusedRow() as ShiftDto;
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
}
