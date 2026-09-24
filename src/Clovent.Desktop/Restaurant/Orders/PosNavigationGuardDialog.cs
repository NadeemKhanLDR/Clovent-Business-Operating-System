using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Professional modal guard dialog displayed when attempting to navigate from
/// Restaurant POS to Back Office while an order is currently in progress.
/// Prevents uncommitted cart items from being accidentally destroyed.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class PosNavigationGuardDialog : XtraForm
{
    private static readonly Color AmberColor = Color.FromArgb(217, 119, 6);
    private static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
    private static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);
    private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);

    private LabelControl _titleLabel = null!;
    private LabelControl _messageLabel = null!;
    private LabelControl _detailLabel = null!;
    private SimpleButton _holdAndOpenButton = null!;
    private SimpleButton _stayInPosButton = null!;

    public PosNavigationGuardDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Order in Progress";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(24, 20, 24, 20),
            BackColor = Color.White
        };

        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Message
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Detail
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

        _titleLabel = new LabelControl
        {
            Text = "ORDER IN PROGRESS",
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 8)
        };
        _titleLabel.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _titleLabel.Appearance.ForeColor = AmberColor;
        _titleLabel.Appearance.Options.UseFont = true;
        _titleLabel.Appearance.Options.UseForeColor = true;

        _messageLabel = new LabelControl
        {
            Text = "You have an order currently in progress.",
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 6)
        };
        _messageLabel.Appearance.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        _messageLabel.Appearance.ForeColor = TextPrimary;
        _messageLabel.Appearance.Options.UseFont = true;
        _messageLabel.Appearance.Options.UseForeColor = true;

        _detailLabel = new LabelControl
        {
            Text = "To protect the order, hold it before opening Back Office, or stay in POS to continue working.",
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.Vertical,
            Margin = new Padding(0, 0, 0, 16)
        };
        _detailLabel.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        _detailLabel.Appearance.ForeColor = TextSecondary;
        _detailLabel.Appearance.Options.UseFont = true;
        _detailLabel.Appearance.Options.UseForeColor = true;

        var buttonFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0)
        };

        _stayInPosButton = new SimpleButton
        {
            Text = "Stay in POS",
            DialogResult = DialogResult.Cancel,
            Height = DesktopDpi.Scale(36, this),
            Width = DesktopDpi.Scale(120, this),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        _stayInPosButton.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _stayInPosButton.Appearance.BackColor = Color.White;
        _stayInPosButton.Appearance.ForeColor = TextPrimary;
        _stayInPosButton.Appearance.BorderColor = BorderColor;
        _stayInPosButton.Appearance.Options.UseFont = true;
        _stayInPosButton.Appearance.Options.UseBackColor = true;
        _stayInPosButton.Appearance.Options.UseForeColor = true;
        _stayInPosButton.Appearance.Options.UseBorderColor = true;
        _stayInPosButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;

        _holdAndOpenButton = new SimpleButton
        {
            Text = "Hold & Open Back Office",
            DialogResult = DialogResult.Yes,
            Height = DesktopDpi.Scale(36, this),
            Width = DesktopDpi.Scale(200, this),
            Margin = new Padding(0),
            Cursor = Cursors.Hand
        };
        _holdAndOpenButton.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _holdAndOpenButton.Appearance.BackColor = AmberColor;
        _holdAndOpenButton.Appearance.ForeColor = Color.White;
        _holdAndOpenButton.Appearance.BorderColor = AmberColor;
        _holdAndOpenButton.Appearance.Options.UseFont = true;
        _holdAndOpenButton.Appearance.Options.UseBackColor = true;
        _holdAndOpenButton.Appearance.Options.UseForeColor = true;
        _holdAndOpenButton.Appearance.Options.UseBorderColor = true;
        _holdAndOpenButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;

        buttonFlow.Controls.Add(_stayInPosButton);
        buttonFlow.Controls.Add(_holdAndOpenButton);

        mainPanel.Controls.Add(_titleLabel, 0, 0);
        mainPanel.Controls.Add(_messageLabel, 0, 1);
        mainPanel.Controls.Add(_detailLabel, 0, 2);
        mainPanel.Controls.Add(buttonFlow, 0, 3);

        Controls.Add(mainPanel);

        AcceptButton = _holdAndOpenButton;
        CancelButton = _stayInPosButton;

        DesktopDialogSizing.Apply(this, 500, 220, 420, 190, null, false);
    }
}
