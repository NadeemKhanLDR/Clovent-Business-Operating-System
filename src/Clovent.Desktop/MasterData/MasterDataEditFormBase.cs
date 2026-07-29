using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData;

/// <summary>
/// Shared chrome for every master-data create/edit dialog: a two-column
/// label+editor content area plus OK/Cancel buttons, with a
/// <see cref="ValidateFields"/> hook a subclass overrides to block OK when
/// the entered data is invalid. Each entity has its own field set, so this
/// deliberately does not try to be generic over fields - only the chrome
/// (layout, buttons, validation gate) is shared, matching this milestone's
/// "reusable... edit-dialog pattern" ask.
/// </summary>
public abstract class MasterDataEditFormBase : XtraForm
{
    private readonly TableLayoutPanel _contentPanel = new() { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, Padding = new Padding(12) };
    private readonly SimpleButton _okButton = new() { Text = "OK" };
    private readonly SimpleButton _cancelButton = new() { Text = "Cancel", DialogResult = DialogResult.Cancel };
    private int _rowCount;

    /// <summary>Builds the dialog shell.</summary>
    protected MasterDataEditFormBase(string title)
    {
        Text = title;
        Width = 480;
        Height = 360;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        _contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        _okButton.Click += (_, _) =>
        {
            if (ValidateFields(out var error))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                XtraMessageBox.Show(this, error, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft };
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_okButton);

        Controls.Add(_contentPanel);
        Controls.Add(buttonPanel);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;
    }

    /// <summary>Adds a labelled field row to the content area.</summary>
    protected void AddField(string label, Control editor)
    {
        _contentPanel.RowCount = _rowCount + 1;
        _contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var labelControl = new LabelControl { Text = label, Padding = new Padding(0, 6, 8, 0) };
        editor.Width = 260;
        editor.Margin = new Padding(0, 3, 0, 3);

        _contentPanel.Controls.Add(labelControl, 0, _rowCount);
        _contentPanel.Controls.Add(editor, 1, _rowCount);
        _rowCount++;
    }

    /// <summary>Validates every field before OK is allowed to close the dialog. The default accepts anything.</summary>
    /// <param name="error">The message to show the user when validation fails.</param>
    protected virtual bool ValidateFields(out string error)
    {
        error = string.Empty;
        return true;
    }
}
