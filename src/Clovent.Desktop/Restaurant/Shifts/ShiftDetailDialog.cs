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
    private LabelControl _lblCardSalesVal = null!;
    private LabelControl _lblOtherSalesVal = null!;
    private LabelControl _lblTotalSalesVal = null!;
    private LabelControl _lblCashInVal = null!;
    private LabelControl _lblCashOutVal = null!;
    private LabelControl _lblExpectedVal = null!;
    private LabelControl _lblCountedVal = null!;
    private LabelControl _lblVarianceVal = null!;
    private LabelControl _lblReasonVal = null!;

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
        ClientSize = new Size(680, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = true;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            RowCount = 5,
            ColumnCount = 1
        };

        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35f));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));

        _lblHeader = new LabelControl
        {
            Text = "Shift Session Details",
            Appearance = { Font = new Font(Font.FontFamily, 12f, FontStyle.Bold), ForeColor = Color.FromArgb(13, 148, 136) },
            Dock = DockStyle.Fill
        };

        // Summary Card
        var grpInfo = new GroupControl { Text = "Shift Summary", Dock = DockStyle.Fill };
        var infoGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            RowCount = 5,
            ColumnCount = 4
        };

        for (int c = 0; c < 4; c++) infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        for (int r = 0; r < 5; r++) infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

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

        infoGrid.Controls.Add(new LabelControl { Text = "Starting Cash:" }, 0, 2);
        _lblStartingVal = new LabelControl { Text = "0.00" };
        infoGrid.Controls.Add(_lblStartingVal, 1, 2);

        infoGrid.Controls.Add(new LabelControl { Text = "Cash Sales:" }, 2, 2);
        _lblCashSalesVal = new LabelControl { Text = "0.00" };
        infoGrid.Controls.Add(_lblCashSalesVal, 3, 2);

        infoGrid.Controls.Add(new LabelControl { Text = "Card Sales:" }, 0, 3);
        _lblCardSalesVal = new LabelControl { Text = "0.00" };
        infoGrid.Controls.Add(_lblCardSalesVal, 1, 3);

        infoGrid.Controls.Add(new LabelControl { Text = "Other Sales:" }, 2, 3);
        _lblOtherSalesVal = new LabelControl { Text = "0.00" };
        infoGrid.Controls.Add(_lblOtherSalesVal, 3, 3);

        infoGrid.Controls.Add(new LabelControl { Text = "Expected Cash:" }, 0, 4);
        _lblExpectedVal = new LabelControl { Text = "0.00", Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(13, 148, 136) } };
        infoGrid.Controls.Add(_lblExpectedVal, 1, 4);

        infoGrid.Controls.Add(new LabelControl { Text = "Counted / Variance:" }, 2, 4);
        _lblVarianceVal = new LabelControl { Text = "0.00", Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) } };
        infoGrid.Controls.Add(_lblVarianceVal, 3, 4);

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
            Padding = new Padding(0, 5, 0, 0)
        };
        _btnClose = new SimpleButton { Text = "Close", DialogResult = DialogResult.OK, Size = new Size(110, 36) };
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
                _lblCashierVal.Text = summary.Shift.CashierName;
                _lblOpenedVal.Text = summary.Shift.OpenedAtUtc.ToLocalTime().ToString("g");
                _lblClosedVal.Text = summary.Shift.ClosedAtUtc.HasValue ? summary.Shift.ClosedAtUtc.Value.ToLocalTime().ToString("g") : "Still Open";

                _lblStartingVal.Text = summary.StartingCash.ToString("N2");
                _lblCashSalesVal.Text = summary.CashSales.ToString("N2");
                _lblCardSalesVal.Text = summary.CardSales.ToString("N2");
                _lblOtherSalesVal.Text = summary.OtherSales.ToString("N2");

                _lblExpectedVal.Text = summary.ExpectedCash.ToString("N2");
                _lblVarianceVal.Text = summary.Shift.Status == "Closed"
                    ? $"{summary.CountedCash:N2} (Variance: {summary.Variance:N2})"
                    : "Shift Open";

                _gridMovements.DataSource = summary.CashMovements;
            }
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to load shift details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
