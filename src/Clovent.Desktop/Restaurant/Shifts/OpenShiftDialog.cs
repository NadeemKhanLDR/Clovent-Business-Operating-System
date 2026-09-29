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

    private LabelControl _lblCashierVal = null!;
    private LabelControl _lblTerminalVal = null!;
    private LabelControl _lblBusinessDateVal = null!;
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
        _businessDate = BusinessDateTimeService.Instance.Today;
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
        _businessDate = businessDate ?? BusinessDateTimeService.Instance.Today;

        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Open Cash Register Shift";
        AutoScaleMode = AutoScaleMode.None;
        DesktopDialogSizing.Apply(this, 540, 410, 480, 360, null, false);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(DesktopDpi.Scale(20, this)),
            RowCount = 5,
            ColumnCount = 1
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(44, this))); // Header title & subtitle
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(84, this))); // Read-only context info
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(44, this))); // Opening cash input
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));                         // Notes memo edit
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(44, this))); // Action buttons

        // --- 1. Header Banner ---
        var headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(4, this))
        };

        var lblTitle = new LabelControl
        {
            Text = "OPEN CASH REGISTER SHIFT",
            Location = new Point(0, 0),
            Appearance =
            {
                Font = new Font(Font.FontFamily, 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(13, 148, 136)
            }
        };

        var lblSubtitle = new LabelControl
        {
            Text = "Start a new cash-register shift",
            Location = new Point(0, DesktopDpi.Scale(22, this)),
            Appearance =
            {
                Font = new Font(Font.FontFamily, 8.75f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139)
            }
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblSubtitle);
        root.Controls.Add(headerPanel, 0, 0);

        // --- 2. Read-only Context (Cashier, Terminal, Business Date) ---
        var contextGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0, DesktopDpi.Scale(2, this), 0, DesktopDpi.Scale(4, this))
        };
        contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(110, this)));
        contextGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (int i = 0; i < 3; i++)
        {
            contextGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(26, this)));
        }

        var lblCashierTitle = new LabelControl
        {
            Text = "Cashier",
            Anchor = AnchorStyles.Left,
            TabStop = false,
            Appearance = { ForeColor = Color.FromArgb(100, 116, 139), Font = new Font(Font.FontFamily, 9.25f, FontStyle.Regular) }
        };
        _lblCashierVal = new LabelControl
        {
            Text = UserDisplayNameHelper.GetCurrentCashierDisplayName(_currentSession),
            Anchor = AnchorStyles.Left,
            TabStop = false,
            Appearance = { ForeColor = Color.FromArgb(15, 23, 42), Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) }
        };

        var lblTerminalTitle = new LabelControl
        {
            Text = "Terminal",
            Anchor = AnchorStyles.Left,
            TabStop = false,
            Appearance = { ForeColor = Color.FromArgb(100, 116, 139), Font = new Font(Font.FontFamily, 9.25f, FontStyle.Regular) }
        };
        _lblTerminalVal = new LabelControl
        {
            Text = $"{_terminalName} ({_branchName})",
            Anchor = AnchorStyles.Left,
            TabStop = false,
            Appearance = { ForeColor = Color.FromArgb(15, 23, 42), Font = new Font(Font.FontFamily, 9.25f, FontStyle.Regular) }
        };

        var lblBusinessDateTitle = new LabelControl
        {
            Text = "Business Date",
            Anchor = AnchorStyles.Left,
            TabStop = false,
            Appearance = { ForeColor = Color.FromArgb(100, 116, 139), Font = new Font(Font.FontFamily, 9.25f, FontStyle.Regular) }
        };
        _lblBusinessDateVal = new LabelControl
        {
            Text = DateTimeDisplay.FormatDate(_businessDate),
            Anchor = AnchorStyles.Left,
            TabStop = false,
            Appearance = { ForeColor = Color.FromArgb(15, 23, 42), Font = new Font(Font.FontFamily, 9.25f, FontStyle.Regular) }
        };

        contextGrid.Controls.Add(lblCashierTitle, 0, 0);
        contextGrid.Controls.Add(_lblCashierVal, 1, 0);
        contextGrid.Controls.Add(lblTerminalTitle, 0, 1);
        contextGrid.Controls.Add(_lblTerminalVal, 1, 1);
        contextGrid.Controls.Add(lblBusinessDateTitle, 0, 2);
        contextGrid.Controls.Add(_lblBusinessDateVal, 1, 2);
        root.Controls.Add(contextGrid, 0, 1);

        // --- 3. Opening Cash Input ---
        var cashRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, DesktopDpi.Scale(4, this), 0, DesktopDpi.Scale(4, this))
        };
        cashRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(110, this)));
        cashRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(180, this)));
        cashRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        cashRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var lblStartingCash = new LabelControl
        {
            Text = "Opening Cash",
            Anchor = AnchorStyles.Left,
            Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) }
        };

        _spnStartingCash = new SpinEdit
        {
            Anchor = AnchorStyles.Left,
            Width = DesktopDpi.Scale(170, this),
            Value = 0m,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            TabIndex = 0
        };
        _spnStartingCash.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        _spnStartingCash.Properties.Mask.EditMask = "n2";
        _spnStartingCash.Properties.Mask.UseMaskAsDisplayFormat = true;
        _spnStartingCash.Properties.MinValue = 0;
        _spnStartingCash.Properties.MaxValue = 1000000;
        _spnStartingCash.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;

        var lblCurrencyBadge = new LabelControl
        {
            Text = CurrencyDisplay.SymbolOrCode,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(DesktopDpi.Scale(8, this), 0, 0, 0),
            Appearance =
            {
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font(Font.FontFamily, 9f, FontStyle.Bold)
            }
        };

        cashRow.Controls.Add(lblStartingCash, 0, 0);
        cashRow.Controls.Add(_spnStartingCash, 1, 0);
        cashRow.Controls.Add(lblCurrencyBadge, 2, 0);
        root.Controls.Add(cashRow, 0, 2);

        // --- 4. Notes Input ---
        var notesPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, DesktopDpi.Scale(4, this), 0, DesktopDpi.Scale(4, this))
        };
        notesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(110, this)));
        notesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        notesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var lblNotes = new LabelControl
        {
            Text = "Notes",
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Appearance = { ForeColor = Color.FromArgb(100, 116, 139), Font = new Font(Font.FontFamily, 9.25f, FontStyle.Regular) },
            Margin = new Padding(0, DesktopDpi.Scale(4, this), 0, 0)
        };
        _txtNotes = new MemoEdit
        {
            Dock = DockStyle.Fill,
            TabIndex = 1
        };
        _txtNotes.Properties.NullValuePrompt = "Optional shift opening notes...";

        notesPanel.Controls.Add(lblNotes, 0, 0);
        notesPanel.Controls.Add(_txtNotes, 1, 0);
        root.Controls.Add(notesPanel, 0, 3);

        // --- 5. Action Buttons ---
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, DesktopDpi.Scale(6, this), 0, 0)
        };

        _btnCancel = new SimpleButton
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(DesktopDpi.Scale(105, this), DesktopDpi.Scale(34, this)),
            TabIndex = 3
        };

        _btnOpen = new SimpleButton
        {
            Text = "Open Shift",
            Size = new Size(DesktopDpi.Scale(125, this), DesktopDpi.Scale(34, this)),
            Appearance = { Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold) },
            TabIndex = 2
        };
        _btnOpen.Appearance.BackColor = Color.FromArgb(13, 148, 136);
        _btnOpen.Appearance.ForeColor = Color.White;
        _btnOpen.Appearance.Options.UseBackColor = true;
        _btnOpen.Appearance.Options.UseForeColor = true;
        _btnOpen.Click += BtnOpen_Click;

        btnPanel.Controls.Add(_btnOpen);
        btnPanel.Controls.Add(_btnCancel);
        root.Controls.Add(btnPanel, 0, 4);

        Controls.Add(root);
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
        var cashierName = UserDisplayNameHelper.FormatCashierName(_lblCashierVal.Text);

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
