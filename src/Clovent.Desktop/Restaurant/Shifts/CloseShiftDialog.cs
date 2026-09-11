using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Application.Shifts.Queries;
using DevExpress.XtraEditors;
using MediatR;

namespace Clovent.Desktop.Restaurant.Shifts;

/// <summary>
/// WinForms / DevExpress dialog for Cash Register Balancing and Shift Closure, featuring a Blind Cash Count workflow.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class CloseShiftDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly Guid _shiftId;

    private ShiftSummaryDto? _summary;

    private LabelControl _lblShiftHeader = null!;
    private LabelControl _lblStartingCashVal = null!;
    private LabelControl _lblCashSalesVal = null!;
    private LabelControl _lblCardSalesVal = null!;
    private LabelControl _lblOtherSalesVal = null!;
    private LabelControl _lblTotalSalesVal = null!;
    private LabelControl _lblCashInVal = null!;
    private LabelControl _lblCashOutVal = null!;
    private LabelControl _lblExpectedCashVal = null!;

    private SpinEdit _spnCountedCash = null!;
    private LabelControl _lblVarianceVal = null!;
    private ComboBoxEdit _cboVarianceReason = null!;
    private MemoEdit _txtNotes = null!;

    private SimpleButton _btnCloseShift = null!;
    private SimpleButton _btnCancel = null!;

    /// <summary>Summary of the closed shift after successful closure.</summary>
    public ShiftSummaryDto? ClosedShiftSummary { get; private set; }

    /// <summary>Design-time constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CloseShiftDialog()
    {
        _mediator = null!;
        _shiftId = Guid.Empty;
        BuildUi();
    }

    /// <summary>Constructs Close Shift Dialog for active shift.</summary>
    public CloseShiftDialog(IMediator mediator, Guid shiftId)
    {
        _mediator = mediator;
        _shiftId = shiftId;
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Close Cash Register Shift & Register Balancing";
        ClientSize = new Size(580, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            RowCount = 4,
            ColumnCount = 1
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); // Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180f)); // Summary Group
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Count & Variance
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f)); // Buttons

        _lblShiftHeader = new LabelControl
        {
            Text = "Shift Session # ---",
            Appearance = { Font = new Font(Font.FontFamily, 12f, FontStyle.Bold), ForeColor = Color.FromArgb(13, 148, 136) },
            Dock = DockStyle.Fill
        };

        // Summary Group Box
        var grpSummary = new GroupControl { Text = "Shift Summary & Register Totals", Dock = DockStyle.Fill };
        var summaryGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            RowCount = 5,
            ColumnCount = 4
        };

        for (int c = 0; c < 4; c++) summaryGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        for (int r = 0; r < 5; r++) summaryGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

        summaryGrid.Controls.Add(new LabelControl { Text = "Starting Cash:" }, 0, 0);
        _lblStartingCashVal = new LabelControl { Text = "0.00", Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) } };
        summaryGrid.Controls.Add(_lblStartingCashVal, 1, 0);

        summaryGrid.Controls.Add(new LabelControl { Text = "Total Sales:" }, 2, 0);
        _lblTotalSalesVal = new LabelControl { Text = "0.00", Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) } };
        summaryGrid.Controls.Add(_lblTotalSalesVal, 3, 0);

        summaryGrid.Controls.Add(new LabelControl { Text = "Cash Sales:" }, 0, 1);
        _lblCashSalesVal = new LabelControl { Text = "0.00" };
        summaryGrid.Controls.Add(_lblCashSalesVal, 1, 1);

        summaryGrid.Controls.Add(new LabelControl { Text = "Card Sales:" }, 2, 1);
        _lblCardSalesVal = new LabelControl { Text = "0.00" };
        summaryGrid.Controls.Add(_lblCardSalesVal, 3, 1);

        summaryGrid.Controls.Add(new LabelControl { Text = "Cash In (+):" }, 0, 2);
        _lblCashInVal = new LabelControl { Text = "0.00" };
        summaryGrid.Controls.Add(_lblCashInVal, 1, 2);

        summaryGrid.Controls.Add(new LabelControl { Text = "Other Sales:" }, 2, 2);
        _lblOtherSalesVal = new LabelControl { Text = "0.00" };
        summaryGrid.Controls.Add(_lblOtherSalesVal, 3, 2);

        summaryGrid.Controls.Add(new LabelControl { Text = "Cash Out (-):" }, 0, 3);
        _lblCashOutVal = new LabelControl { Text = "0.00" };
        summaryGrid.Controls.Add(_lblCashOutVal, 1, 3);

        summaryGrid.Controls.Add(new LabelControl { Text = "Expected Cash:" }, 0, 4);
        _lblExpectedCashVal = new LabelControl { Text = "0.00", Appearance = { Font = new Font(Font.FontFamily, 10f, FontStyle.Bold), ForeColor = Color.FromArgb(13, 148, 136) } };
        summaryGrid.Controls.Add(_lblExpectedCashVal, 1, 4);

        grpSummary.Controls.Add(summaryGrid);

        // Count & Variance Box
        var grpCount = new GroupControl { Text = "Blind Cash Count & Reconciliation", Dock = DockStyle.Fill };
        var countGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            RowCount = 4,
            ColumnCount = 2
        };

        countGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
        countGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        countGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
        countGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 35f));
        countGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
        countGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var lblCountedCash = new LabelControl { Text = "Counted Cash:", Anchor = AnchorStyles.Left, Appearance = { Font = new Font(Font.FontFamily, 10f, FontStyle.Bold) } };
        _spnCountedCash = new SpinEdit
        {
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 11f, FontStyle.Bold),
            Value = 0m
        };
        _spnCountedCash.Properties.Mask.EditMask = "c2";
        _spnCountedCash.Properties.MinValue = 0;
        _spnCountedCash.Properties.MaxValue = 1000000;
        _spnCountedCash.ValueChanged += SpnCountedCash_ValueChanged;

        var lblVarianceHeader = new LabelControl { Text = "Variance:", Anchor = AnchorStyles.Left };
        _lblVarianceVal = new LabelControl
        {
            Text = "0.00",
            Anchor = AnchorStyles.Left,
            Appearance = { Font = new Font(Font.FontFamily, 11f, FontStyle.Bold) }
        };

        var lblReason = new LabelControl { Text = "Variance Reason:", Anchor = AnchorStyles.Left };
        _cboVarianceReason = new ComboBoxEdit
        {
            Dock = DockStyle.Fill,
            Properties =
            {
                TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard,
                Items =
                {
                    "Balanced / No Variance",
                    "Cash Shortage",
                    "Cash Overage",
                    "Incorrect Change Provided",
                    "Paid Out Note Omitted",
                    "Other / Unexplained"
                }
            }
        };

        var lblNotes = new LabelControl { Text = "Notes:", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        _txtNotes = new MemoEdit { Dock = DockStyle.Fill };

        countGrid.Controls.Add(lblCountedCash, 0, 0);
        countGrid.Controls.Add(_spnCountedCash, 1, 0);

        countGrid.Controls.Add(lblVarianceHeader, 0, 1);
        countGrid.Controls.Add(_lblVarianceVal, 1, 1);

        countGrid.Controls.Add(lblReason, 0, 2);
        countGrid.Controls.Add(_cboVarianceReason, 1, 2);

        countGrid.Controls.Add(lblNotes, 0, 3);
        countGrid.Controls.Add(_txtNotes, 1, 3);

        grpCount.Controls.Add(countGrid);

        // Action Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0)
        };

        _btnCancel = new SimpleButton { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(100, 38) };
        _btnCloseShift = new SimpleButton
        {
            Text = "Close Shift",
            Size = new Size(140, 38),
            Appearance = { Font = new Font(Font.FontFamily, 10f, FontStyle.Bold), ForeColor = Color.FromArgb(220, 38, 38) }
        };
        _btnCloseShift.Click += BtnCloseShift_Click;

        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnCloseShift);

        mainLayout.Controls.Add(_lblShiftHeader, 0, 0);
        mainLayout.Controls.Add(grpSummary, 0, 1);
        mainLayout.Controls.Add(grpCount, 0, 2);
        mainLayout.Controls.Add(btnPanel, 0, 3);

        Controls.Add(mainLayout);
        AcceptButton = _btnCloseShift;
        CancelButton = _btnCancel;

        Load += CloseShiftDialog_Load;

        AppearanceManager.Apply(this, "Restaurant", nameof(CloseShiftDialog));
    }

    private async void CloseShiftDialog_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode) return;

        try
        {
            _summary = await _mediator.Send(new GetShiftSummaryQuery(_shiftId));
            if (_summary != null)
            {
                _lblShiftHeader.Text = $"Shift #{_summary.Shift.ShiftNumber} - Cashier: {_summary.Shift.CashierName} ({_summary.Shift.OpenedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm})";

                _lblStartingCashVal.Text = _summary.StartingCash.ToString("N2");
                _lblCashSalesVal.Text = _summary.CashSales.ToString("N2");
                _lblCardSalesVal.Text = _summary.CardSales.ToString("N2");
                _lblOtherSalesVal.Text = _summary.OtherSales.ToString("N2");
                _lblTotalSalesVal.Text = _summary.TotalSales.ToString("N2");
                _lblCashInVal.Text = _summary.CashIn.ToString("N2");
                _lblCashOutVal.Text = _summary.CashOut.ToString("N2");
                _lblExpectedCashVal.Text = _summary.ExpectedCash.ToString("N2");

                _spnCountedCash.Value = _summary.ExpectedCash;
                UpdateVarianceDisplay();
            }
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to load shift summary: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SpnCountedCash_ValueChanged(object? sender, EventArgs e)
    {
        UpdateVarianceDisplay();
    }

    private void UpdateVarianceDisplay()
    {
        if (_summary == null) return;

        var counted = _spnCountedCash.Value;
        var expected = _summary.ExpectedCash;
        var variance = counted - expected;

        _lblVarianceVal.Text = variance.ToString("N2");
        if (variance == 0)
        {
            _lblVarianceVal.Appearance.ForeColor = Color.FromArgb(22, 163, 74);
            _cboVarianceReason.Text = "Balanced / No Variance";
        }
        else if (variance < 0)
        {
            _lblVarianceVal.Appearance.ForeColor = Color.FromArgb(220, 38, 38);
            if (_cboVarianceReason.Text == "Balanced / No Variance")
                _cboVarianceReason.Text = "Cash Shortage";
        }
        else
        {
            _lblVarianceVal.Appearance.ForeColor = Color.FromArgb(217, 119, 6);
            if (_cboVarianceReason.Text == "Balanced / No Variance")
                _cboVarianceReason.Text = "Cash Overage";
        }
    }

    private async void BtnCloseShift_Click(object? sender, EventArgs e)
    {
        if (_summary == null) return;

        var counted = _spnCountedCash.Value;
        var expected = _summary.ExpectedCash;
        var variance = counted - expected;

        if (variance != 0 && string.IsNullOrWhiteSpace(_cboVarianceReason.Text))
        {
            XtraMessageBox.Show(this, "A variance reason is required when counted cash differs from expected cash.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnCloseShift.Enabled = false;
        try
        {
            var command = new CloseShiftCommand(
                _shiftId,
                counted,
                variance != 0 ? _cboVarianceReason.Text.Trim() : null,
                _txtNotes.Text);

            ClosedShiftSummary = await _mediator.Send(command);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to close shift: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnCloseShift.Enabled = true;
        }
    }
}
