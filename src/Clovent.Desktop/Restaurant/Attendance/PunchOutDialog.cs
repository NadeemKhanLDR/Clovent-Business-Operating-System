using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.Shifts;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.Attendance.Commands;
using Clovent.Restaurant.Application.Attendance.Dtos;
using Clovent.Restaurant.Application.Attendance.Services;
using Clovent.Restaurant.Application.Shifts.Dtos;
using DevExpress.XtraEditors;
using MediatR;

namespace Clovent.Desktop.Restaurant.Attendance;

/// <summary>
/// Professional WinForms / DevExpress dialog for employee working-time Punch Out (Clock Out).
/// Enforces that an employee cannot punch out while owning an open cash register shift.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class PunchOutDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly ICurrentSession _currentSession;
    private readonly IAttendanceAccessService _attendanceAccessService;
    private readonly AttendanceSessionDto _session;
    private readonly Guid? _terminalId;

    private ShiftDto? _activeShift;

    private TextEdit _txtEmployee = null!;
    private TextEdit _txtPunchedIn = null!;
    private TextEdit _txtCurrentTime = null!;
    private TextEdit _txtDuration = null!;
    private PanelControl _pnlShiftWarning = null!;
    private LabelControl _lblShiftWarningText = null!;
    private MemoEdit _txtNotes = null!;
    private SimpleButton _btnCloseShift = null!;
    private SimpleButton _btnPunchOut = null!;
    private SimpleButton _btnCancel = null!;

    /// <summary>Whether punch-out succeeded.</summary>
    public bool PunchedOut { get; private set; }

    /// <summary>The closed attendance session after punch out.</summary>
    public AttendanceSessionDto? ClosedSession { get; private set; }

    /// <summary>Design-time constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public PunchOutDialog()
    {
        _mediator = null!;
        _currentSession = null!;
        _attendanceAccessService = null!;
        _session = null!;
        _terminalId = null!;
        BuildUi();
    }

    /// <summary>Constructs the Punch Out Dialog.</summary>
    public PunchOutDialog(
        IMediator mediator,
        ICurrentSession currentSession,
        IAttendanceAccessService attendanceAccessService,
        AttendanceSessionDto session,
        Guid? terminalId = null)
    {
        _mediator = mediator;
        _currentSession = currentSession;
        _attendanceAccessService = attendanceAccessService;
        _session = session;
        _terminalId = terminalId;

        BuildUi();
        Load += PunchOutDialog_Load;
    }

    private void BuildUi()
    {
        Text = "Employee Punch Out";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        DesktopDialogSizing.Apply(this, 540, 520, 460, 440, null, false);

        var nowUtc = DateTimeOffset.UtcNow;
        var punchInUtc = _session?.PunchInAtUtc ?? nowUtc;
        var duration = nowUtc - punchInUtc;
        if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(DesktopDpi.Scale(16, this)),
            RowCount = 8,
            ColumnCount = 2
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(130, this)));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        for (int i = 0; i < 4; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(38, this)));
        }
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(68, this))); // Shift warning
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Notes
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(48, this))); // Buttons

        var lblEmployee = new LabelControl { Text = "Employee:", Anchor = AnchorStyles.Left };
        _txtEmployee = new TextEdit
        {
            Text = UserDisplayNameHelper.FormatCashierName(_session?.UserName) != "Cashier"
                ? UserDisplayNameHelper.FormatCashierName(_session?.UserName)
                : UserDisplayNameHelper.GetCurrentCashierDisplayName(_currentSession),
            ReadOnly = true,
            Dock = DockStyle.Fill
        };
        _txtEmployee.Properties.ReadOnly = true;

        var lblPunchedIn = new LabelControl { Text = "Punched In:", Anchor = AnchorStyles.Left };
        _txtPunchedIn = new TextEdit
        {
            Text = _session != null ? DateTimeDisplay.FormatDateTime(_session.PunchInAtUtc) : "-",
            ReadOnly = true,
            Dock = DockStyle.Fill
        };
        _txtPunchedIn.Properties.ReadOnly = true;

        var lblCurrentTime = new LabelControl { Text = "Current Time:", Anchor = AnchorStyles.Left };
        _txtCurrentTime = new TextEdit
        {
            Text = DateTimeDisplay.FormatDateTime(nowUtc),
            ReadOnly = true,
            Dock = DockStyle.Fill
        };
        _txtCurrentTime.Properties.ReadOnly = true;

        var lblDuration = new LabelControl { Text = "Worked Duration:", Anchor = AnchorStyles.Left, Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) } };
        _txtDuration = new TextEdit
        {
            Text = $"{(int)duration.TotalHours}h {duration.Minutes:D2}m",
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold)
        };
        _txtDuration.Properties.ReadOnly = true;

        // Warning panel for open cash shift
        _pnlShiftWarning = new PanelControl
        {
            Dock = DockStyle.Fill,
            Visible = false,
            Appearance = { BackColor = Color.FromArgb(254, 242, 242) } // Soft red/rose
        };
        _pnlShiftWarning.Appearance.Options.UseBackColor = true;

        _lblShiftWarningText = new LabelControl
        {
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None,
            Padding = new Padding(DesktopDpi.Scale(8, this)),
            Text = "CLOSE SHIFT FIRST: You still have an open POS shift. Close your cash shift before punching out.",
            Appearance = { ForeColor = Color.FromArgb(185, 28, 28), Font = new Font(Font.FontFamily, 9f, FontStyle.Bold) }
        };
        _lblShiftWarningText.Appearance.Options.UseForeColor = true;
        _lblShiftWarningText.Appearance.Options.UseFont = true;
        _pnlShiftWarning.Controls.Add(_lblShiftWarningText);

        var lblNotes = new LabelControl { Text = "Closing Notes:", Anchor = AnchorStyles.Top | AnchorStyles.Left };
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

        _btnCloseShift = new SimpleButton
        {
            Text = "Close Shift...",
            Visible = false,
            Size = new Size(DesktopDpi.Scale(120, this), DesktopDpi.Scale(36, this)),
            Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) }
        };
        _btnCloseShift.Appearance.BackColor = Color.FromArgb(220, 38, 38); // Crimson
        _btnCloseShift.Appearance.ForeColor = Color.White;
        _btnCloseShift.Appearance.Options.UseBackColor = true;
        _btnCloseShift.Appearance.Options.UseForeColor = true;
        _btnCloseShift.Click += BtnCloseShift_Click;

        _btnPunchOut = new SimpleButton
        {
            Text = "Punch Out",
            Size = new Size(DesktopDpi.Scale(120, this), DesktopDpi.Scale(36, this)),
            Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) }
        };
        _btnPunchOut.Appearance.BackColor = Color.FromArgb(15, 118, 110); // Teal
        _btnPunchOut.Appearance.ForeColor = Color.White;
        _btnPunchOut.Appearance.Options.UseBackColor = true;
        _btnPunchOut.Appearance.Options.UseForeColor = true;
        _btnPunchOut.Click += BtnPunchOut_Click;

        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnCloseShift);
        btnPanel.Controls.Add(_btnPunchOut);

        panel.Controls.Add(lblEmployee, 0, 0);
        panel.Controls.Add(_txtEmployee, 1, 0);

        panel.Controls.Add(lblPunchedIn, 0, 1);
        panel.Controls.Add(_txtPunchedIn, 1, 1);

        panel.Controls.Add(lblCurrentTime, 0, 2);
        panel.Controls.Add(_txtCurrentTime, 1, 2);

        panel.Controls.Add(lblDuration, 0, 3);
        panel.Controls.Add(_txtDuration, 1, 3);

        panel.Controls.Add(_pnlShiftWarning, 0, 4);
        panel.SetColumnSpan(_pnlShiftWarning, 2);

        panel.Controls.Add(lblNotes, 0, 5);
        panel.Controls.Add(_txtNotes, 1, 5);

        panel.Controls.Add(btnPanel, 0, 6);
        panel.SetColumnSpan(btnPanel, 2);

        Controls.Add(panel);
        CancelButton = _btnCancel;
    }

    private async void PunchOutDialog_Load(object? sender, EventArgs e)
    {
        await EvaluateShiftStatusAsync();
    }

    private async System.Threading.Tasks.Task EvaluateShiftStatusAsync()
    {
        var userId = _session?.UserId ?? _currentSession?.UserId ?? Guid.Empty;
        if (userId == Guid.Empty) return;

        try
        {
            _activeShift = await _attendanceAccessService.GetActiveShiftForUserAsync(userId);
            if (_activeShift != null)
            {
                _lblShiftWarningText.Text = $"CLOSE SHIFT FIRST\n\nYou still have an open POS shift (Shift #{_activeShift.ShiftNumber}).\nPlease close your cash shift before punching out.";
                _pnlShiftWarning.Visible = true;
                _btnPunchOut.Enabled = false;
                _btnCloseShift.Visible = true;
            }
            else
            {
                _pnlShiftWarning.Visible = false;
                _btnPunchOut.Enabled = true;
                _btnCloseShift.Visible = false;
                AcceptButton = _btnPunchOut;
            }
        }
        catch
        {
            // Fallback - proceed with default enabled
            _btnPunchOut.Enabled = true;
            _btnCloseShift.Visible = false;
        }
    }

    private async void BtnCloseShift_Click(object? sender, EventArgs e)
    {
        if (_activeShift == null) return;

        using var closeDialog = new CloseShiftDialog(_mediator, _activeShift.ShiftId);
        var result = closeDialog.ShowDialog(this);
        if (result == DialogResult.OK && closeDialog.ClosedShiftSummary != null)
        {
            // Shift was closed! Re-evaluate shift status
            await EvaluateShiftStatusAsync();
        }
    }

    private async void BtnPunchOut_Click(object? sender, EventArgs e)
    {
        var userId = _session?.UserId ?? _currentSession?.UserId ?? Guid.Empty;
        var userName = _session?.UserName ?? _currentSession?.DisplayName ?? "Administrator";

        _btnPunchOut.Enabled = false;
        _btnCancel.Enabled = false;

        try
        {
            var command = new PunchOutCommand(
                userId,
                userName,
                _terminalId,
                string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim());

            ClosedSession = await _mediator.Send(command);
            PunchedOut = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _btnPunchOut.Enabled = true;
            _btnCancel.Enabled = true;
            XtraMessageBox.Show(this, ex.Message, "Punch Out Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
