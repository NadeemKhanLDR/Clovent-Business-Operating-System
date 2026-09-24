using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.Application.Shifts.Dtos;
using DevExpress.XtraEditors;
using MediatR;

namespace Clovent.Desktop.Restaurant.Shifts;

/// <summary>
/// Professional WinForms / DevExpress dialog to open a new cash register shift session.
/// Enforces cashier identity, terminal context, and business date awareness.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class OpenShiftDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly ICurrentSession _currentSession;

    private readonly Guid _branchId;
    private readonly Guid _warehouseId;
    private readonly Guid _terminalId;
    private readonly string _terminalName;
    private readonly string _branchName;
    private readonly DateOnly _businessDate;

    private TextEdit _txtCashierName = null!;
    private TextEdit _txtTerminal = null!;
    private TextEdit _txtBusinessDate = null!;
    private SpinEdit _spnStartingCash = null!;
    private MemoEdit _txtNotes = null!;
    private SimpleButton _btnOpen = null!;
    private SimpleButton _btnCancel = null!;

    /// <summary>The opened shift DTO after successful submission.</summary>
    public ShiftDto? OpenedShift { get; private set; }

    /// <summary>Design-time constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public OpenShiftDialog()
    {
        _mediator = null!;
        _currentSession = null!;
        _branchId = Guid.Empty;
        _warehouseId = Guid.Empty;
        _terminalId = Guid.Empty;
        _terminalName = "T-001";
        _branchName = "Main Branch";
        _businessDate = DateOnly.FromDateTime(DateTime.Today);
        BuildUi();
    }

    /// <summary>Constructs the Open Shift Dialog.</summary>
    public OpenShiftDialog(
        IMediator mediator,
        ICurrentSession currentSession,
        Guid branchId,
        Guid warehouseId,
        Guid terminalId,
        string? terminalName = null,
        string? branchName = null,
        DateOnly? businessDate = null)
    {
        _mediator = mediator;
        _currentSession = currentSession;
        _branchId = branchId;
        _warehouseId = warehouseId;
        _terminalId = terminalId;
        _terminalName = string.IsNullOrWhiteSpace(terminalName) ? "POS Terminal" : terminalName;
        _branchName = string.IsNullOrWhiteSpace(branchName) ? "Main Branch" : branchName;
        _businessDate = businessDate ?? DateOnly.FromDateTime(DateTime.Today);

        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Open Cash Register Shift";
        DesktopDialogSizing.Apply(this, 540, 480, 480, 420, null, false);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(DesktopDpi.Scale(16, this)),
            RowCount = 7,
            ColumnCount = 2
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(130, this)));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        for (int i = 0; i < 5; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(40, this)));
        }
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Notes
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(48, this))); // Buttons

        var lblCashier = new LabelControl { Text = "Cashier:", Anchor = AnchorStyles.Left };
        _txtCashierName = new TextEdit
        {
            Text = UserDisplayNameHelper.GetCurrentCashierDisplayName(_currentSession),
            ReadOnly = true,
            Dock = DockStyle.Fill
        };
        _txtCashierName.Properties.ReadOnly = true;

        var lblTerminal = new LabelControl { Text = "Terminal:", Anchor = AnchorStyles.Left };
        _txtTerminal = new TextEdit
        {
            Text = $"{_terminalName} ({_branchName})",
            ReadOnly = true,
            Dock = DockStyle.Fill
        };
        _txtTerminal.Properties.ReadOnly = true;

        var lblBusinessDate = new LabelControl { Text = "Business Date:", Anchor = AnchorStyles.Left };
        _txtBusinessDate = new TextEdit
        {
            Text = DateTimeDisplay.FormatDate(_businessDate),
            ReadOnly = true,
            Dock = DockStyle.Fill
        };
        _txtBusinessDate.Properties.ReadOnly = true;

        var lblStartingCash = new LabelControl { Text = $"Opening Cash ({CurrencyDisplay.SymbolOrCode}):", Anchor = AnchorStyles.Left, Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) } };
        _spnStartingCash = new SpinEdit
        {
            Dock = DockStyle.Fill,
            Value = 0m,
            Font = new Font(Font.FontFamily, 10.5f, FontStyle.Bold)
        };
        _spnStartingCash.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        _spnStartingCash.Properties.Mask.EditMask = "n2";
        _spnStartingCash.Properties.Mask.UseMaskAsDisplayFormat = true;
        _spnStartingCash.Properties.MinValue = 0;
        _spnStartingCash.Properties.MaxValue = 1000000;

        var lblNotes = new LabelControl { Text = "Notes:", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        _txtNotes = new MemoEdit { Dock = DockStyle.Fill };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, DesktopDpi.Scale(8, this), 0, 0)
        };

        _btnCancel = new SimpleButton
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(DesktopDpi.Scale(100, this), DesktopDpi.Scale(38, this))
        };

        _btnOpen = new SimpleButton
        {
            Text = "Open Shift",
            Size = new Size(DesktopDpi.Scale(140, this), DesktopDpi.Scale(38, this)),
            Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) }
        };
        _btnOpen.Appearance.BackColor = Color.FromArgb(13, 148, 136);
        _btnOpen.Appearance.ForeColor = Color.White;
        _btnOpen.Appearance.Options.UseBackColor = true;
        _btnOpen.Appearance.Options.UseForeColor = true;
        _btnOpen.Click += BtnOpen_Click;

        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnOpen);

        panel.Controls.Add(lblCashier, 0, 0);
        panel.Controls.Add(_txtCashierName, 1, 0);

        panel.Controls.Add(lblTerminal, 0, 1);
        panel.Controls.Add(_txtTerminal, 1, 1);

        panel.Controls.Add(lblBusinessDate, 0, 2);
        panel.Controls.Add(_txtBusinessDate, 1, 2);

        panel.Controls.Add(lblStartingCash, 0, 3);
        panel.Controls.Add(_spnStartingCash, 1, 3);

        panel.Controls.Add(lblNotes, 0, 4);
        panel.Controls.Add(_txtNotes, 1, 4);
        panel.SetRowSpan(_txtNotes, 2);

        panel.Controls.Add(btnPanel, 0, 6);
        panel.SetColumnSpan(btnPanel, 2);

        Controls.Add(panel);
        AcceptButton = _btnOpen;
        CancelButton = _btnCancel;

        AppearanceManager.Apply(this, "Restaurant", nameof(OpenShiftDialog));
    }

    private async void BtnOpen_Click(object? sender, EventArgs e)
    {
        var startingCash = _spnStartingCash.Value;
        if (startingCash < 0)
        {
            XtraMessageBox.Show(this, "Opening cash cannot be negative.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cashierId = _currentSession?.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var cashierName = UserDisplayNameHelper.FormatCashierName(_txtCashierName.Text);

        _btnOpen.Enabled = false;
        try
        {
            var command = new OpenShiftCommand(
                _branchId,
                _warehouseId,
                _terminalId,
                cashierId,
                cashierName,
                startingCash,
                _txtNotes.Text);

            OpenedShift = await _mediator.Send(command);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to open shift: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnOpen.Enabled = true;
        }
    }
}
