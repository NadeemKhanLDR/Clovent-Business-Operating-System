using Clovent.Desktop.Forms.Base;
using Clovent.Restaurant.Application.Tables.Dtos;
using DevExpress.XtraEditors;
using System.Drawing;
using System.Windows.Forms;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Compact dialog shown during Quick Order Dine-In flow when no active order exists.
/// Displays available tables using the POS table picker format, protecting occupied tables.
/// </summary>
public sealed class SelectTableDialog : XtraForm
{
    private static readonly Color AccentColor = Color.FromArgb(13, 148, 136);
    private static readonly Color MutedColor = Color.FromArgb(71, 85, 105);
    private static readonly Color BorderColor = Color.FromArgb(203, 213, 225);

    private readonly IReadOnlyCollection<TableDto> _tables;
    private readonly ComboBoxEdit _tableCombo;

    /// <summary>The ID of the table selected by the cashier, or null if cancelled.</summary>
    public Guid? SelectedTableId { get; private set; }

    /// <summary>Initializes a new instance of <see cref="SelectTableDialog"/>.</summary>
    public SelectTableDialog(IReadOnlyCollection<TableDto> tables, Control? owner = null)
    {
        _tables = tables;

        Text = "Select Table";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        BackColor = Color.White;
        AutoScaleMode = AutoScaleMode.None;

        var scale = (int v) => DesktopDpi.Scale(v, owner ?? this);
        ClientSize = new Size(scale(380), scale(195));

        var promptLabel = new LabelControl
        {
            Text = "Select an available table for this Dine-In order:",
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.None,
            Height = scale(40),
            Padding = new Padding(scale(20), scale(16), scale(20), 0)
        };
        promptLabel.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        promptLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        promptLabel.Appearance.Options.UseFont = true;
        promptLabel.Appearance.Options.UseForeColor = true;

        var comboContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = scale(46),
            Padding = new Padding(scale(20), scale(4), scale(20), scale(4)),
            BackColor = Color.White
        };

        _tableCombo = new ComboBoxEdit
        {
            Dock = DockStyle.Fill
        };
        _tableCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _tableCombo.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        _tableCombo.Properties.Appearance.BorderColor = BorderColor;
        _tableCombo.Properties.Appearance.Options.UseBorderColor = true;
        _tableCombo.Properties.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _tableCombo.Properties.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _tableCombo.Properties.Appearance.Options.UseFont = true;
        _tableCombo.Properties.Appearance.Options.UseForeColor = true;

        var activeTables = tables
            .Where(t => string.Equals(t.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Code)
            .ToList();

        int defaultIndex = -1;
        int i = 0;
        foreach (var t in activeTables)
        {
            _tableCombo.Properties.Items.Add($"{t.Code} ({t.OccupancyStatus})");
            if (defaultIndex < 0 && string.Equals(t.OccupancyStatus, "Available", StringComparison.OrdinalIgnoreCase))
            {
                defaultIndex = i;
            }
            i++;
        }

        if (defaultIndex >= 0)
        {
            _tableCombo.SelectedIndex = defaultIndex;
        }
        else if (_tableCombo.Properties.Items.Count > 0)
        {
            _tableCombo.SelectedIndex = 0;
        }

        comboContainer.Controls.Add(_tableCombo);

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = scale(54),
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(scale(16), scale(4), scale(16), scale(12)),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var btnSelect = new SimpleButton
        {
            Text = "Select Table",
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
        };
        btnSelect.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnSelect.Appearance.BackColor = AccentColor;
        btnSelect.Appearance.ForeColor = Color.White;
        btnSelect.Appearance.Options.UseFont = true;
        btnSelect.Appearance.Options.UseBackColor = true;
        btnSelect.Appearance.Options.UseForeColor = true;
        btnSelect.Click += (_, _) =>
        {
            var idx = _tableCombo.SelectedIndex;
            if (idx < 0 || idx >= activeTables.Count)
            {
                XtraMessageBox.Show(this, "Please select a table.", "No Table Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var chosen = activeTables[idx];
            if (!string.Equals(chosen.OccupancyStatus, "Available", StringComparison.OrdinalIgnoreCase))
            {
                XtraMessageBox.Show(this,
                    $"Table {chosen.Code} is {chosen.OccupancyStatus} and cannot receive a new order. Please choose an available table.",
                    "Table Unavailable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            SelectedTableId = chosen.TableId;
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
            SelectedTableId = null;
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonPanel.Controls.Add(btnSelect, 0, 0);
        buttonPanel.Controls.Add(btnCancel, 1, 0);

        var divider = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Color.FromArgb(226, 232, 240)
        };

        Controls.Add(promptLabel);
        Controls.Add(comboContainer);
        Controls.Add(divider);
        Controls.Add(buttonPanel);

        AcceptButton = btnSelect;
        CancelButton = btnCancel;
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                SelectedTableId = null;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
    }
}
