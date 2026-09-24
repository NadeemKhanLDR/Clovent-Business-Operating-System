using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.Attendance.Commands;
using Clovent.Restaurant.Application.Attendance.Dtos;
using DevExpress.XtraEditors;
using MediatR;

namespace Clovent.Desktop.Restaurant.Attendance;

/// <summary>
/// Professional WinForms / DevExpress dialog for employee working-time Punch In (Clock In).
/// This dialog captures attendance working-time only.
/// Does NOT request opening cash float or till drawer values.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class PunchInDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly ICurrentSession _currentSession;
    private readonly Guid _branchId;
    private readonly string _branchName;
    private readonly Guid? _terminalId;

    private TextEdit _txtEmployee = null!;
    private TextEdit _txtLocation = null!;
    private TextEdit _txtDate = null!;
    private TextEdit _txtTime = null!;
    private MemoEdit _txtNotes = null!;
    private SimpleButton _btnPunchIn = null!;
    private SimpleButton _btnCancel = null!;

    /// <summary>Whether punch-in succeeded.</summary>
    public bool PunchedIn { get; private set; }

    /// <summary>The resulting attendance session after punch in.</summary>
    public AttendanceSessionDto? Session { get; private set; }

    /// <summary>Design-time constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public PunchInDialog()
    {
        _mediator = null!;
        _currentSession = null!;
        _branchId = Guid.Empty;
        _branchName = "Main Branch";
        _terminalId = null;
        BuildUi();
    }

    /// <summary>Constructs the Punch In Dialog.</summary>
    public PunchInDialog(
        IMediator mediator,
        ICurrentSession currentSession,
        Guid branchId,
        string? branchName = null,
        Guid? terminalId = null)
    {
        _mediator = mediator;
        _currentSession = currentSession;
        _branchId = branchId;
        _branchName = string.IsNullOrWhiteSpace(branchName) ? "Main Branch" : branchName;
        _terminalId = terminalId;

        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Employee Punch In";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        DesktopDialogSizing.Apply(this, 500, 440, 440, 380, null, false);

        var nowLocal = DateTime.Now;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(DesktopDpi.Scale(16, this)),
            RowCount = 7,
            ColumnCount = 2
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(120, this)));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        for (int i = 0; i < 4; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(38, this)));
        }
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Notes
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(48, this))); // Buttons

        var lblEmployee = new LabelControl { Text = "Employee:", Anchor = AnchorStyles.Left };
        _txtEmployee = new TextEdit
        {
            Text = UserDisplayNameHelper.GetCurrentCashierDisplayName(_currentSession),
            ReadOnly = true,
            Dock = DockStyle.Fill
        };
        _txtEmployee.Properties.ReadOnly = true;

        var lblLocation = new LabelControl { Text = "Location:", Anchor = AnchorStyles.Left };
        _txtLocation = new TextEdit
        {
            Text = _branchName,
            ReadOnly = true,
            Dock = DockStyle.Fill
        };
        _txtLocation.Properties.ReadOnly = true;

        var lblDate = new LabelControl { Text = "Date:", Anchor = AnchorStyles.Left };
        _txtDate = new TextEdit
        {
            Text = DateTimeDisplay.FormatDate(DateTimeOffset.UtcNow),
            ReadOnly = true,
            Dock = DockStyle.Fill
        };
        _txtDate.Properties.ReadOnly = true;

        var lblTime = new LabelControl { Text = "Time:", Anchor = AnchorStyles.Left };
        _txtTime = new TextEdit
        {
            Text = DateTimeDisplay.FormatTime(DateTimeOffset.UtcNow),
            ReadOnly = true,
            Dock = DockStyle.Fill
        };
        _txtTime.Properties.ReadOnly = true;

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
            Size = new Size(DesktopDpi.Scale(100, this), DesktopDpi.Scale(36, this))
        };

        _btnPunchIn = new SimpleButton
        {
            Text = "Punch In",
            Size = new Size(DesktopDpi.Scale(120, this), DesktopDpi.Scale(36, this)),
            Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) }
        };
        _btnPunchIn.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Emerald / Teal
        _btnPunchIn.Appearance.ForeColor = Color.White;
        _btnPunchIn.Appearance.Options.UseBackColor = true;
        _btnPunchIn.Appearance.Options.UseForeColor = true;
        _btnPunchIn.Click += BtnPunchIn_Click;

        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnPunchIn);

        panel.Controls.Add(lblEmployee, 0, 0);
        panel.Controls.Add(_txtEmployee, 1, 0);

        panel.Controls.Add(lblLocation, 0, 1);
        panel.Controls.Add(_txtLocation, 1, 1);

        panel.Controls.Add(lblDate, 0, 2);
        panel.Controls.Add(_txtDate, 1, 2);

        panel.Controls.Add(lblTime, 0, 3);
        panel.Controls.Add(_txtTime, 1, 3);

        panel.Controls.Add(lblNotes, 0, 4);
        panel.Controls.Add(_txtNotes, 1, 4);

        panel.Controls.Add(btnPanel, 0, 5);
        panel.SetColumnSpan(btnPanel, 2);

        Controls.Add(panel);
        AcceptButton = _btnPunchIn;
        CancelButton = _btnCancel;
    }

    private async void BtnPunchIn_Click(object? sender, EventArgs e)
    {
        var userId = _currentSession?.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userName = _currentSession?.DisplayName ?? "Administrator";

        _btnPunchIn.Enabled = false;
        _btnCancel.Enabled = false;

        try
        {
            var command = new PunchInCommand(
                userId,
                userName,
                _branchId,
                _branchName,
                _terminalId,
                string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim());

            Session = await _mediator.Send(command);
            PunchedIn = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _btnPunchIn.Enabled = true;
            _btnCancel.Enabled = true;
            XtraMessageBox.Show(this, ex.Message, "Punch In Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
