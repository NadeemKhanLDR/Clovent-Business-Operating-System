namespace Clovent.Desktop.Inventory.WarehouseStocks;

partial class QuantityPromptForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

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
        label1 = new System.Windows.Forms.Label();
        _quantityEdit = new DevExpress.XtraEditors.SpinEdit();
        ((System.ComponentModel.ISupportInitialize)_quantityEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 1;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_quantityEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _quantityEdit
        _quantityEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _quantityEdit
        //
        _quantityEdit.Name = "_quantityEdit";
        _quantityEdit.Properties.MinValue = 0.0001m;
        _quantityEdit.Properties.MaxValue = 1_000_000;
        //
        // QuantityPromptForm
        //
        ((System.ComponentModel.ISupportInitialize)_quantityEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.SpinEdit _quantityEdit;

    private System.Windows.Forms.Label label1;
}
