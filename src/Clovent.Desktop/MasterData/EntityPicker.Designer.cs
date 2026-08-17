using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace Clovent.Desktop.MasterData;

partial class EntityPicker
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly ComboBoxEdit _combo = new();
    private readonly LabelControl _label = new();
    private readonly FlowLayoutPanel _layout = new();

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
        SuspendLayout();
        //
        // _combo
        //
        _combo.Name = "_combo";
        _combo.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;
        _combo.SelectedIndexChanged += Combo_SelectedIndexChanged;
        //
        // _label
        //
        _label.Name = "_label";
        _label.Padding = new Padding(0, 6, 4, 0);
        //
        // _layout
        //
        // layout is deliberately NOT Dock=Top: this control's own Width is
        // determined by AutoSize wrapping *around* layout, and a Dock=Top
        // child instead has its Width forced to match ITS PARENT's Width -
        // the same circular AutoSize dependency already identified for
        // MasterDataListView's toolbar (a Dock=Fill/Top child contributes
        // nothing toward an AutoSize parent's preferred size). That
        // circularity is harmless when this whole control stays Dock=Top
        // itself (its own Width then legitimately comes from ITS parent),
        // but breaks - collapsing to zero width - when a caller clears
        // Dock to embed this picker inline in a FlowLayoutPanel toolbar
        // (RestaurantPosView's Warehouse/Table pickers were invisible for
        // exactly this reason, confirmed via manual verification).
        _layout.FlowDirection = FlowDirection.LeftToRight;
        _layout.WrapContents = false;
        _layout.AutoSize = true;
        _layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _layout.Controls.Add(_label);
        _layout.Controls.Add(_combo);
        _layout.Name = "_layout";
        _layout.SizeChanged += Layout_SizeChanged;
        //
        // EntityPicker
        //
        // AutoSize (not a fixed Height=32) so this always fits its actual
        // rendered content - a fixed height was measured too short for a
        // DevExpress ComboBoxEdit's real height under this environment's
        // DPI/skin, overlapping whatever was docked below it (the same
        // class of bug fixed in MasterDataListView's toolbar; see that
        // type's BuildLayout for the full explanation).
        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Name = "EntityPicker";
        Controls.Add(_layout);
        ((System.ComponentModel.ISupportInitialize)_combo.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion
}
