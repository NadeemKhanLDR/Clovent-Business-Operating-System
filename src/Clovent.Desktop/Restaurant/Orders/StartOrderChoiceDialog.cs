using Clovent.Desktop.Forms.Base;
using DevExpress.XtraEditors;
using System.Drawing;
using System.Windows.Forms;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Choice for how to start an order when adding a deal with no active order.
/// </summary>
public enum StartOrderChoice
{
    /// <summary>Cancel order creation and keep deal preview open.</summary>
    Cancel,
    /// <summary>Start a Dine-In order and assign a table.</summary>
    DineIn,
    /// <summary>Start a Take Away order directly.</summary>
    TakeAway
}

/// <summary>
/// Compact choice dialog shown when a cashier clicks "Add Deal" with no active order.
/// Prompts the cashier to start a Dine-In or Take Away order, or cancel cleanly.
/// </summary>
public sealed class StartOrderChoiceDialog : XtraForm
{
    private static readonly Color AccentColor = Color.FromArgb(13, 148, 136);
    private static readonly Color MutedColor = Color.FromArgb(71, 85, 105);
    private static readonly Color BorderColor = Color.FromArgb(203, 213, 225);

    /// <summary>The cashier's selected start order choice.</summary>
    public StartOrderChoice Choice { get; private set; } = StartOrderChoice.Cancel;

    /// <summary>Initializes a new instance of <see cref="StartOrderChoiceDialog"/>.</summary>
    public StartOrderChoiceDialog(Control? owner = null)
    {
        Text = "Start Order";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        BackColor = Color.White;
        AutoScaleMode = AutoScaleMode.None;

        var scale = (int v) => DesktopDpi.Scale(v, owner ?? this);
        ClientSize = new Size(scale(380), scale(160));

        var promptLabel = new LabelControl
        {
            Text = "No active order exists.\r\nHow would you like to start this order?",
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.None,
            Height = scale(65),
            Padding = new Padding(scale(20), scale(16), scale(20), 0)
        };
        promptLabel.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        promptLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        promptLabel.Appearance.Options.UseFont = true;
        promptLabel.Appearance.Options.UseForeColor = true;
        promptLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        promptLabel.Appearance.Options.UseTextOptions = true;

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = scale(54),
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(scale(16), scale(4), scale(16), scale(12)),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var btnDineIn = new SimpleButton
        {
            Text = "Dine-In",
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
        };
        btnDineIn.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnDineIn.Appearance.BackColor = AccentColor;
        btnDineIn.Appearance.ForeColor = Color.White;
        btnDineIn.Appearance.Options.UseFont = true;
        btnDineIn.Appearance.Options.UseBackColor = true;
        btnDineIn.Appearance.Options.UseForeColor = true;
        btnDineIn.Click += (_, _) =>
        {
            Choice = StartOrderChoice.DineIn;
            DialogResult = DialogResult.OK;
            Close();
        };

        var btnTakeAway = new SimpleButton
        {
            Text = "Take Away",
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
        };
        btnTakeAway.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnTakeAway.Appearance.BackColor = AccentColor;
        btnTakeAway.Appearance.ForeColor = Color.White;
        btnTakeAway.Appearance.Options.UseFont = true;
        btnTakeAway.Appearance.Options.UseBackColor = true;
        btnTakeAway.Appearance.Options.UseForeColor = true;
        btnTakeAway.Click += (_, _) =>
        {
            Choice = StartOrderChoice.TakeAway;
            DialogResult = DialogResult.OK;
            Close();
        };

        var btnCancel = new SimpleButton
        {
            Text = "Cancel",
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
        };
        btnCancel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnCancel.Appearance.BackColor = Color.White;
        btnCancel.Appearance.ForeColor = MutedColor;
        btnCancel.Appearance.BorderColor = BorderColor;
        btnCancel.Appearance.Options.UseFont = true;
        btnCancel.Appearance.Options.UseBackColor = true;
        btnCancel.Appearance.Options.UseForeColor = true;
        btnCancel.Appearance.Options.UseBorderColor = true;
        btnCancel.Click += (_, _) =>
        {
            Choice = StartOrderChoice.Cancel;
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonPanel.Controls.Add(btnDineIn, 0, 0);
        buttonPanel.Controls.Add(btnTakeAway, 1, 0);
        buttonPanel.Controls.Add(btnCancel, 2, 0);

        var divider = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Color.FromArgb(226, 232, 240)
        };

        Controls.Add(promptLabel);
        Controls.Add(divider);
        Controls.Add(buttonPanel);

        CancelButton = btnCancel;
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Choice = StartOrderChoice.Cancel;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
    }
}
