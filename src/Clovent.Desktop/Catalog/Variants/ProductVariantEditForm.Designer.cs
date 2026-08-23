namespace Clovent.Desktop.Catalog.Variants;

partial class ProductVariantEditForm
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
        label1 = new DevExpress.XtraEditors.LabelControl();
        label2 = new DevExpress.XtraEditors.LabelControl();
        label3 = new DevExpress.XtraEditors.LabelControl();
        _nameEdit = new DevExpress.XtraEditors.TextEdit();
        _skuEdit = new DevExpress.XtraEditors.TextEdit();
        _unitCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_skuEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_unitCombo.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 3;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_nameEdit, 1, 0);
        // label1
        label1.Text = "Name:";
        label1.Padding = new System.Windows.Forms.Padding(0, 6, 8, 0);
        // _nameEdit
        _nameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _nameEdit.Width = 260;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_skuEdit, 1, 1);
        // label2
        label2.Text = "SKU:";
        label2.Padding = new System.Windows.Forms.Padding(0, 6, 8, 0);
        // _skuEdit
        _skuEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _skuEdit.Width = 260;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_unitCombo, 1, 2);
        // label3
        label3.Text = "Unit:";
        label3.Padding = new System.Windows.Forms.Padding(0, 6, 8, 0);
        // _unitCombo
        _unitCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _unitCombo.Width = 260;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _nameEdit
        //
        _nameEdit.Name = "_nameEdit";
        //
        // _skuEdit
        //
        _skuEdit.Name = "_skuEdit";
        //
        // _unitCombo
        //
        _unitCombo.Name = "_unitCombo";
        //
        // ProductVariantEditForm
        //



        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_skuEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_unitCombo.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.TextEdit _nameEdit;
    private DevExpress.XtraEditors.TextEdit _skuEdit;
    private DevExpress.XtraEditors.ComboBoxEdit _unitCombo;

    private DevExpress.XtraEditors.LabelControl label1;
    private DevExpress.XtraEditors.LabelControl label2;
    private DevExpress.XtraEditors.LabelControl label3;
}
