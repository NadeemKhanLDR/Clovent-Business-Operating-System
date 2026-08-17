namespace Clovent.Desktop.Catalog.Barcodes;

partial class BarcodeCreateForm
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
        _valueEdit = new DevExpress.XtraEditors.TextEdit();
        _isPrimaryEdit = new DevExpress.XtraEditors.CheckEdit();
        ((System.ComponentModel.ISupportInitialize)_valueEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_isPrimaryEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 2;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_valueEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Value (8-14 digits):";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _valueEdit
        _valueEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(_isPrimaryEdit, 0, 1);
        _contentPanel.SetColumnSpan(_isPrimaryEdit, 2);
        // _isPrimaryEdit
        _isPrimaryEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _valueEdit
        //
        _valueEdit.Name = "_valueEdit";
        //
        // _isPrimaryEdit
        //
        _isPrimaryEdit.Name = "_isPrimaryEdit";
        _isPrimaryEdit.Text = "Set as primary barcode";
        //
        // BarcodeCreateForm
        //


        ((System.ComponentModel.ISupportInitialize)_valueEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_isPrimaryEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.TextEdit _valueEdit;
    private DevExpress.XtraEditors.CheckEdit _isPrimaryEdit;

    private System.Windows.Forms.Label label1;
}
