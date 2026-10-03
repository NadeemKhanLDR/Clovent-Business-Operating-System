using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace Clovent.Desktop.MasterData;

partial class EntityPicker
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly ComboBoxEdit _combo = new();
    private readonly LabelControl _label = new();
    private readonly TableLayoutPanel _layout = new();

    /// <summary>Clean up any resources being used.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        ((System.ComponentModel.ISupportInitialize)_combo.Properties).BeginInit();
        _layout.SuspendLayout();
        SuspendLayout();
        //
        // _combo
        //
        _combo.Name = "_combo";
        _combo.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;
        _combo.SelectedIndexChanged += Combo_SelectedIndexChanged;
        _combo.Anchor = AnchorStyles.Left;
        _combo.Margin = Padding.Empty;
        //
        // _label
        //
        _label.Name = "_label";
        _label.Anchor = AnchorStyles.Left;
        _label.Margin = new Padding(0, 0, 4, 0);
        _label.Padding = Padding.Empty;
        _label.AutoSizeMode = LabelAutoSizeMode.Horizontal;
        //
        // _layout
        //
        _layout.ColumnCount = 2;
        _layout.RowCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.AutoSize = true;
        _layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _layout.Controls.Add(_label, 0, 0);
        _layout.Controls.Add(_combo, 1, 0);
        _layout.Name = "_layout";
        _layout.SizeChanged += Layout_SizeChanged;
        //
        // EntityPicker
        //
        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Name = "EntityPicker";
        Controls.Add(_layout);
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_combo.Properties).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
