using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// Professional modal dialog for configuring POS Active Orders rail wait-time health thresholds
/// (Green/Orange, in minutes), persisted locally via <see cref="PosSettingsStore"/>.
/// </summary>
public sealed class OrderHealthSettingsForm : XtraForm
{
    private readonly SpinEdit _greenMinutesEdit = new();
    private readonly SpinEdit _orangeMinutesEdit = new();
    private readonly SimpleButton _saveButton = new();
    private readonly SimpleButton _cancelButton = new();

    /// <summary>Builds the dialog pre-loaded with the persisted thresholds.</summary>
    public OrderHealthSettingsForm()
    {
        Text = "Order Health Settings";
        Font = new Font("Segoe UI", 9.5F);

        DesktopDialogSizing.Apply(this, 580, 420, 500, 360, null, false);

        var (green, orange) = PosSettingsStore.LoadOrderHealthThresholds();
        ConfigureMinutes(_greenMinutesEdit, green);
        ConfigureMinutes(_orangeMinutesEdit, orange);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(24, 18, 24, 18),
            Margin = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Header
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Threshold Editors
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));        // Status Legend Card
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(48, this))); // Action Buttons

        // --- 1. HEADER ---
        var headerPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14)
        };

        var titleLabel = new LabelControl
        {
            Text = "ORDER HEALTH SETTINGS",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 0, 0, 3)
        };

        var subtitleLabel = new LabelControl
        {
            Text = "Configure when orders change health status based on waiting time.",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139),
            Margin = new Padding(0)
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        root.Controls.Add(headerPanel, 0, 0);

        // --- 2. THRESHOLD EDITORS ---
        var editorsTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 14)
        };
        editorsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(220, this)));
        editorsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        editorsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(36, this)));
        editorsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(36, this)));

        var lblGreen = new LabelControl
        {
            Text = "Green Threshold (minutes):",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Dock = DockStyle.Fill,
            Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } }
        };

        var lblOrange = new LabelControl
        {
            Text = "Orange Threshold (minutes):",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Dock = DockStyle.Fill,
            Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } }
        };

        editorsTable.Controls.Add(lblGreen, 0, 0);
        editorsTable.Controls.Add(_greenMinutesEdit, 1, 0);
        editorsTable.Controls.Add(lblOrange, 0, 1);
        editorsTable.Controls.Add(_orangeMinutesEdit, 1, 1);
        root.Controls.Add(editorsTable, 0, 1);

        // --- 3. STATUS EXPLANATORY CARD ---
        var legendCard = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 0, 14)
        };

        var legendLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0)
        };
        legendLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(80, this)));
        legendLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        legendLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        legendLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        legendLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));

        var tagGreen = new LabelControl
        {
            Text = "● Healthy",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 163, 74), // Green-600
            Dock = DockStyle.Fill,
            Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } }
        };
        var descGreen = new LabelControl
        {
            Text = "Orders under the green threshold are within standard waiting time.",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            Dock = DockStyle.Fill,
            Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } }
        };

        var tagOrange = new LabelControl
        {
            Text = "● Waiting",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(217, 119, 6), // Amber-600
            Dock = DockStyle.Fill,
            Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } }
        };
        var descOrange = new LabelControl
        {
            Text = "Orders between green and orange thresholds require kitchen attention.",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            Dock = DockStyle.Fill,
            Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } }
        };

        var tagRed = new LabelControl
        {
            Text = "● Delayed",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(220, 38, 38), // Red-600
            Dock = DockStyle.Fill,
            Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } }
        };
        var descRed = new LabelControl
        {
            Text = "Orders above the orange threshold require urgent attention.",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            Dock = DockStyle.Fill,
            Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } }
        };

        legendLayout.Controls.Add(tagGreen, 0, 0);
        legendLayout.Controls.Add(descGreen, 1, 0);
        legendLayout.Controls.Add(tagOrange, 0, 1);
        legendLayout.Controls.Add(descOrange, 1, 1);
        legendLayout.Controls.Add(tagRed, 0, 2);
        legendLayout.Controls.Add(descRed, 1, 2);
        legendCard.Controls.Add(legendLayout);
        root.Controls.Add(legendCard, 0, 2);

        // --- 4. ACTION BUTTONS ---
        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };

        _saveButton.Text = "Save Settings";
        _saveButton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _saveButton.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _saveButton.Appearance.ForeColor = Color.White;
        _saveButton.Appearance.Options.UseBackColor = true;
        _saveButton.Appearance.Options.UseForeColor = true;
        _saveButton.Size = new Size(DesktopDpi.Scale(130, this), DesktopDpi.Scale(36, this));
        _saveButton.Click += SaveButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        _cancelButton.Size = new Size(DesktopDpi.Scale(100, this), DesktopDpi.Scale(36, this));
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Margin = new Padding(0, 0, 10, 0);

        buttonPanel.Controls.Add(_saveButton);
        buttonPanel.Controls.Add(_cancelButton);
        root.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(root);
        AcceptButton = _saveButton;
        CancelButton = _cancelButton;
    }

    private void ConfigureMinutes(SpinEdit editor, int value)
    {
        editor.Properties.IsFloatValue = false;
        editor.Properties.MinValue = 1;
        editor.Properties.MaxValue = 24 * 60;
        editor.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        editor.Properties.Appearance.Options.UseFont = true;
        editor.EditValue = value;
        editor.Dock = DockStyle.Fill;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        var green = (int)_greenMinutesEdit.Value;
        var orange = (int)_orangeMinutesEdit.Value;

        if (green <= 0 || orange <= 0 || green >= orange)
        {
            XtraMessageBox.Show(this, "The green threshold must be smaller than the orange threshold, and both must be positive.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        PosSettingsStore.SaveOrderHealthThresholds(green, orange);
        DialogResult = DialogResult.OK;
        Close();
    }
}
