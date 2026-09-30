using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Application.Shifts.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;

namespace Clovent.Desktop.Restaurant.Shifts;

/// <summary>
/// Professional read-only WinForms / DevExpress detail viewer for historical or active shift sessions.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class ShiftDetailDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly Guid _shiftId;

    private LabelControl _lblHeader = null!;
    private LabelControl _lblStatusVal = null!;
    private LabelControl _lblCashierVal = null!;
    private LabelControl _lblOpenedVal = null!;
    private LabelControl _lblClosedVal = null!;

    private LabelControl _lblStartingVal = null!;
    private LabelControl _lblCashSalesVal = null!;
    private LabelControl _lblCashCollectionsVal = null!;
    private LabelControl _lblCardSalesVal = null!;
    private LabelControl _lblOtherSalesVal = null!;
    private LabelControl _lblExpectedVal = null!;
    private LabelControl _lblCountedVal = null!;
    private LabelControl _lblVarianceVal = null!;

    private GridControl _gridMovements = null!;
    private GridView _viewMovements = null!;
    private SimpleButton _btnClose = null!;

    /// <summary>Design-time constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ShiftDetailDialog()
    {
        _mediator = null!;
        _shiftId = Guid.Empty;
        BuildUi();
    }

    /// <summary>Constructs Shift Detail Dialog for a shift ID.</summary>
    public ShiftDetailDialog(IMediator mediator, Guid shiftId)
    {
        _mediator = mediator;
        _shiftId = shiftId;
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Shift Session Details";
        AutoScaleMode = AutoScaleMode.None;
        DesktopDialogSizing.Apply(this, 760, 660, 680, 560, null, true);

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(DesktopDpi.Scale(16, this)),
            RowCount = 4,
            ColumnCount = 1
        };

        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(35, this)));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(240, this)));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(48, this)));

        _lblHeader = new LabelControl
        {
            Text = "Shift Session Details",
            Appearance = { Font = new Font(Font.FontFamily, 12f, FontStyle.Bold), ForeColor = Color.FromArgb(13, 148, 136) },
            Dock = DockStyle.Fill
        };

        // Summary Card
        var grpInfo = new GroupControl { Text = "Shift Summary & Cash Drawer Reconciliation", Dock = DockStyle.Fill };
        var infoGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            RowCount = 6,
            ColumnCount = 4
        };

        infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
        infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
        infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
        infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
        for (int r = 0; r < 6; r++) infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66f));

        infoGrid.Controls.Add(new LabelControl { Text = "Status:" }, 0, 0);
        _lblStatusVal = new LabelControl { Text = "---", Appearance = { Font = new Font(Font.FontFamily, 9f, FontStyle.Bold) } };
        infoGrid.Controls.Add(_lblStatusVal, 1, 0);

        infoGrid.Controls.Add(new LabelControl { Text = "Cashier:" }, 2, 0);
        _lblCashierVal = new LabelControl { Text = "---" };
        infoGrid.Controls.Add(_lblCashierVal, 3, 0);

        infoGrid.Controls.Add(new LabelControl { Text = "Opened At:" }, 0, 1);
        _lblOpenedVal = new LabelControl { Text = "---" };
        infoGrid.Controls.Add(_lblOpenedVal, 1, 1);

        infoGrid.Controls.Add(new LabelControl { Text = "Closed At:" }, 2, 1);
        _lblClosedVal = new LabelControl { Text = "---" };
        infoGrid.Controls.Add(_lblClosedVal, 3, 1);

        infoGrid.Controls.Add(new LabelControl { Text = "Starting Float:" }, 0, 2);
        _lblStartingVal = new LabelControl { Text = "0.00" };
        infoGrid.Controls.Add(_lblStartingVal, 1, 2);

        infoGrid.Controls.Add(new LabelControl { Text = "Cash Sales:" }, 2, 2);
        _lblCashSalesVal = new LabelControl { Text = "0.00" };
        infoGrid.Controls.Add(_lblCashSalesVal, 3, 2);

        infoGrid.Controls.Add(new LabelControl { Text = "Cash Collections:" }, 0, 3);
        _lblCashCollectionsVal = new LabelControl { Text = "0.00" };
        infoGrid.Controls.Add(_lblCashCollectionsVal, 1, 3);

        infoGrid.Controls.Add(new LabelControl { Text = "Card / Non-Cash:" }, 2, 3);
        _lblCardSalesVal = new LabelControl { Text = "0.00" };
        infoGrid.Controls.Add(_lblCardSalesVal, 3, 3);

        infoGrid.Controls.Add(new LabelControl { Text = "Expected Cash:" }, 0, 4);
        _lblExpectedVal = new LabelControl { Text = "0.00", Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(13, 148, 136) } };
        infoGrid.Controls.Add(_lblExpectedVal, 1, 4);

        infoGrid.Controls.Add(new LabelControl { Text = "Counted Cash:" }, 2, 4);
        _lblCountedVal = new LabelControl { Text = "0.00", Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) } };
        infoGrid.Controls.Add(_lblCountedVal, 3, 4);

        infoGrid.Controls.Add(new LabelControl { Text = "Cash Variance:" }, 0, 5);
        _lblVarianceVal = new LabelControl { Text = "0.00", Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) } };
        infoGrid.Controls.Add(_lblVarianceVal, 1, 5);

        grpInfo.Controls.Add(infoGrid);

        // Movements Grid
        var grpMovements = new GroupControl { Text = "Cash Movements (Cash In / Cash Out)", Dock = DockStyle.Fill };
        _gridMovements = new GridControl { Dock = DockStyle.Fill };
        _viewMovements = new GridView(_gridMovements)
        {
            OptionsBehavior = { Editable = false },
            OptionsView = { ShowGroupPanel = false, ShowFooter = true }
        };
        _gridMovements.MainView = _viewMovements;
        grpMovements.Controls.Add(_gridMovements);

        // Close Button
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, DesktopDpi.Scale(6, this), 0, 0)
        };
        _btnClose = new SimpleButton { Text = "Close", DialogResult = DialogResult.OK, Size = new Size(DesktopDpi.Scale(110, this), DesktopDpi.Scale(36, this)) };
        btnPanel.Controls.Add(_btnClose);

        mainPanel.Controls.Add(_lblHeader, 0, 0);
        mainPanel.Controls.Add(grpInfo, 0, 1);
        mainPanel.Controls.Add(grpMovements, 0, 2);
        mainPanel.Controls.Add(btnPanel, 0, 3);

        Controls.Add(mainPanel);
        CancelButton = _btnClose;

        Load += ShiftDetailDialog_Load;

        AppearanceManager.Apply(this, "Restaurant", nameof(ShiftDetailDialog));
    }

    private async void ShiftDetailDialog_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode) return;

        try
        {
            var summary = await _mediator.Send(new GetShiftSummaryQuery(_shiftId));
            if (summary != null)
            {
                _lblHeader.Text = $"Shift #{summary.Shift.ShiftNumber} Details";
                _lblStatusVal.Text = summary.Shift.Status;
                _lblCashierVal.Text = UserDisplayNameHelper.FormatCashierName(summary.Shift.CashierName);
                _lblOpenedVal.Text = DateTimeDisplay.FormatDateTime(summary.Shift.OpenedAtUtc);
                _lblClosedVal.Text = summary.Shift.ClosedAtUtc.HasValue ? DateTimeDisplay.FormatDateTime(summary.Shift.ClosedAtUtc.Value) : "Still Open";

                _lblStartingVal.Text = CurrencyDisplay.Format(summary.StartingCash);
                _lblCashSalesVal.Text = CurrencyDisplay.Format(summary.CashSales);
                _lblCashCollectionsVal.Text = CurrencyDisplay.Format(summary.CashCollections);
                _lblCardSalesVal.Text = CurrencyDisplay.Format(summary.CardSales + summary.OtherSales);

                _lblExpectedVal.Text = CurrencyDisplay.Format(summary.ExpectedCash);
                _lblCountedVal.Text = summary.Shift.Status == "Closed"
                    ? CurrencyDisplay.Format(summary.CountedCash)
                    : "Shift Open";

                if (summary.Shift.Status == "Closed")
                {
                    _lblVarianceVal.Text = CurrencyDisplay.Format(summary.Variance);
                    _lblVarianceVal.Appearance.ForeColor = summary.Variance == 0m
                        ? Color.FromArgb(13, 148, 136)
                        : (summary.Variance < 0 ? Color.FromArgb(220, 38, 38) : Color.FromArgb(37, 99, 235));
                }
                else
                {
                    _lblVarianceVal.Text = "-";
                    _lblVarianceVal.Appearance.ForeColor = Color.Empty;
                }

                _gridMovements.DataSource = summary.CashMovements;
            }
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to load shift details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
