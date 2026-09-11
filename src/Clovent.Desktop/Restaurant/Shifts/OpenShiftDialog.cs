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
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class OpenShiftDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly ICurrentSession _currentSession;

    private readonly Guid _branchId;
    private readonly Guid _warehouseId;
    private readonly Guid _terminalId;

    private TextEdit _txtCashierName = null!;
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
        BuildUi();
    }

    /// <summary>Constructs the Open Shift Dialog.</summary>
    public OpenShiftDialog(
        IMediator mediator,
        ICurrentSession currentSession,
        Guid branchId,
        Guid warehouseId,
        Guid terminalId)
    {
        _mediator = mediator;
        _currentSession = currentSession;
        _branchId = branchId;
        _warehouseId = warehouseId;
        _terminalId = terminalId;

        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Open Cash Register Shift";
        ClientSize = new Size(480, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            RowCount = 5,
            ColumnCount = 2
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        for (int i = 0; i < 4; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
        }
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var lblCashier = new LabelControl { Text = "Cashier:", Anchor = AnchorStyles.Left };
        _txtCashierName = new TextEdit
        {
            Text = _currentSession?.DisplayName ?? "Cashier",
            ReadOnly = true,
            Dock = DockStyle.Fill
        };

        var lblStartingCash = new LabelControl { Text = "Starting Cash:", Anchor = AnchorStyles.Left };
        _spnStartingCash = new SpinEdit
        {
            Dock = DockStyle.Fill,
            Value = 0m
        };
        _spnStartingCash.Properties.Mask.EditMask = "c2";
        _spnStartingCash.Properties.MinValue = 0;
        _spnStartingCash.Properties.MaxValue = 1000000;

        var lblNotes = new LabelControl { Text = "Opening Notes:", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        _txtNotes = new MemoEdit { Dock = DockStyle.Fill };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0)
        };

        _btnCancel = new SimpleButton
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(100, 38)
        };

        _btnOpen = new SimpleButton
        {
            Text = "Open Shift",
            Size = new Size(130, 38),
            Appearance = { Font = new Font(Font.FontFamily, 10f, FontStyle.Bold) }
        };
        _btnOpen.Click += BtnOpen_Click;

        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnOpen);

        panel.Controls.Add(lblCashier, 0, 0);
        panel.Controls.Add(_txtCashierName, 1, 0);

        panel.Controls.Add(lblStartingCash, 0, 1);
        panel.Controls.Add(_spnStartingCash, 1, 1);

        panel.Controls.Add(lblNotes, 0, 2);
        panel.Controls.Add(_txtNotes, 1, 2);
        panel.SetRowSpan(_txtNotes, 2);

        panel.Controls.Add(btnPanel, 0, 4);
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
            XtraMessageBox.Show(this, "Starting cash cannot be negative.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cashierId = _currentSession?.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var cashierName = string.IsNullOrWhiteSpace(_txtCashierName.Text) ? "Cashier" : _txtCashierName.Text.Trim();

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
