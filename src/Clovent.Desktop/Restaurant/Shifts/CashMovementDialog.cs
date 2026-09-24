using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Shifts;
using DevExpress.XtraEditors;
using MediatR;

namespace Clovent.Desktop.Restaurant.Shifts;

/// <summary>
/// WinForms / DevExpress dialog for controlled cash drawer movements (Cash In / Cash Out).
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class CashMovementDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly ICurrentSession _currentSession;
    private readonly Guid _shiftId;

    private RadioGroup _rgType = null!;
    private SpinEdit _spnAmount = null!;
    private TextEdit _txtReason = null!;
    private MemoEdit _txtNotes = null!;
    private SimpleButton _btnSave = null!;
    private SimpleButton _btnCancel = null!;

    /// <summary>Recorded cash movement result.</summary>
    public CashMovementDto? RecordedMovement { get; private set; }

    /// <summary>Design-time constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CashMovementDialog()
    {
        _mediator = null!;
        _currentSession = null!;
        _shiftId = Guid.Empty;
        BuildUi();
    }

    /// <summary>Constructs Cash Movement dialog for active shift.</summary>
    public CashMovementDialog(IMediator mediator, ICurrentSession currentSession, Guid shiftId)
    {
        _mediator = mediator;
        _currentSession = currentSession;
        _shiftId = shiftId;
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Record Cash In / Cash Out";
        DesktopDialogSizing.Apply(this, 520, 440, 460, 380, null, false);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(DesktopDpi.Scale(16, this)),
            RowCount = 5,
            ColumnCount = 2
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(140, this)));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(45, this)));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(45, this)));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(45, this)));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(90, this)));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var lblType = new LabelControl { Text = "Movement Type:", Anchor = AnchorStyles.Left };
        _rgType = new RadioGroup
        {
            Dock = DockStyle.Fill,
            Properties =
            {
                Items =
                {
                    new DevExpress.XtraEditors.Controls.RadioGroupItem(CashMovementType.CashIn, "Cash In (Add Cash)"),
                    new DevExpress.XtraEditors.Controls.RadioGroupItem(CashMovementType.CashOut, "Cash Out (Remove Cash)")
                }
            }
        };
        _rgType.SelectedIndex = 0;

        var lblAmount = new LabelControl { Text = $"Amount ({CurrencyDisplay.SymbolOrCode}):", Anchor = AnchorStyles.Left };
        _spnAmount = new SpinEdit
        {
            Dock = DockStyle.Fill,
            Value = 10m
        };
        _spnAmount.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        _spnAmount.Properties.Mask.EditMask = "n2";
        _spnAmount.Properties.Mask.UseMaskAsDisplayFormat = true;
        _spnAmount.Properties.MinValue = 0.01m;
        _spnAmount.Properties.MaxValue = 1000000;

        var lblReason = new LabelControl { Text = "Reason:", Anchor = AnchorStyles.Left };
        _txtReason = new TextEdit { Dock = DockStyle.Fill };

        var lblNotes = new LabelControl { Text = "Notes:", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        _txtNotes = new MemoEdit { Dock = DockStyle.Fill };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, DesktopDpi.Scale(8, this), 0, 0)
        };

        _btnCancel = new SimpleButton { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(DesktopDpi.Scale(100, this), DesktopDpi.Scale(38, this)) };
        _btnSave = new SimpleButton
        {
            Text = "Save Movement",
            Size = new Size(DesktopDpi.Scale(140, this), DesktopDpi.Scale(38, this)),
            Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) }
        };
        _btnSave.Appearance.BackColor = Color.FromArgb(13, 148, 136);
        _btnSave.Appearance.ForeColor = Color.White;
        _btnSave.Appearance.Options.UseBackColor = true;
        _btnSave.Appearance.Options.UseForeColor = true;
        _btnSave.Click += BtnSave_Click;

        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnSave);

        panel.Controls.Add(lblType, 0, 0);
        panel.Controls.Add(_rgType, 1, 0);

        panel.Controls.Add(lblAmount, 0, 1);
        panel.Controls.Add(_spnAmount, 1, 1);

        panel.Controls.Add(lblReason, 0, 2);
        panel.Controls.Add(_txtReason, 1, 2);

        panel.Controls.Add(lblNotes, 0, 3);
        panel.Controls.Add(_txtNotes, 1, 3);

        panel.Controls.Add(btnPanel, 0, 4);
        panel.SetColumnSpan(btnPanel, 2);

        Controls.Add(panel);
        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        AppearanceManager.Apply(this, "Restaurant", nameof(CashMovementDialog));
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_spnAmount.Value <= 0)
        {
            XtraMessageBox.Show(this, "Amount must be greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtReason.Text))
        {
            XtraMessageBox.Show(this, "Please enter a reason for the cash movement.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var type = (CashMovementType)_rgType.EditValue;
        var userId = _currentSession?.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");

        _btnSave.Enabled = false;
        try
        {
            var command = new RecordCashMovementCommand(
                _shiftId,
                type,
                _spnAmount.Value,
                _txtReason.Text.Trim(),
                userId,
                _txtNotes.Text);

            RecordedMovement = await _mediator.Send(command);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to record cash movement: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSave.Enabled = true;
        }
    }
}
