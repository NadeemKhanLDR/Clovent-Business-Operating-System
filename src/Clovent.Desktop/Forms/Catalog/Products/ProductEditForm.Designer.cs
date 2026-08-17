namespace Clovent.Desktop.Forms.Catalog.Products;

partial class ProductEditForm
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
        label2 = new System.Windows.Forms.Label();
        label3 = new System.Windows.Forms.Label();
        label4 = new System.Windows.Forms.Label();
        label5 = new System.Windows.Forms.Label();
        label6 = new System.Windows.Forms.Label();
        label7 = new System.Windows.Forms.Label();
        _nameEdit = new DevExpress.XtraEditors.TextEdit();
        _skuEdit = new DevExpress.XtraEditors.TextEdit();
        _categoryCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _groupCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _brandCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _unitCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _taxRateEdit = new DevExpress.XtraEditors.SpinEdit();
        _taxInclusiveEdit = new DevExpress.XtraEditors.CheckEdit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_skuEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_categoryCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_groupCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_brandCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_unitCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_taxRateEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_taxInclusiveEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 8;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_nameEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Name:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _nameEdit
        _nameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_skuEdit, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "SKU:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _skuEdit
        _skuEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_categoryCombo, 1, 2);
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "Category:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _categoryCombo
        _categoryCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label4, 0, 3);
        _contentPanel.Controls.Add(_groupCombo, 1, 3);
        // label4
        label4.AutoSize = true;
        label4.Dock = System.Windows.Forms.DockStyle.Left;
        label4.Text = "Group:";
        label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label4.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _groupCombo
        _groupCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label5, 0, 4);
        _contentPanel.Controls.Add(_brandCombo, 1, 4);
        // label5
        label5.AutoSize = true;
        label5.Dock = System.Windows.Forms.DockStyle.Left;
        label5.Text = "Brand:";
        label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label5.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _brandCombo
        _brandCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label6, 0, 5);
        _contentPanel.Controls.Add(_unitCombo, 1, 5);
        // label6
        label6.AutoSize = true;
        label6.Dock = System.Windows.Forms.DockStyle.Left;
        label6.Text = "Base Unit:";
        label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label6.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _unitCombo
        _unitCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label7, 0, 6);
        _contentPanel.Controls.Add(_taxRateEdit, 1, 6);
        // label7
        label7.AutoSize = true;
        label7.Dock = System.Windows.Forms.DockStyle.Left;
        label7.Text = "Tax Rate (%):";
        label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label7.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _taxRateEdit
        _taxRateEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(_taxInclusiveEdit, 0, 7);
        _contentPanel.SetColumnSpan(_taxInclusiveEdit, 2);
        // _taxInclusiveEdit
        _taxInclusiveEdit.Dock = System.Windows.Forms.DockStyle.Top;
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
        // _categoryCombo
        //
        _categoryCombo.Name = "_categoryCombo";
        //
        // _groupCombo
        //
        _groupCombo.Name = "_groupCombo";
        //
        // _brandCombo
        //
        _brandCombo.Name = "_brandCombo";
        //
        // _unitCombo
        //
        _unitCombo.Name = "_unitCombo";
        //
        // _taxRateEdit
        //
        _taxRateEdit.Name = "_taxRateEdit";
        _taxRateEdit.Properties.MinValue = 0;
        _taxRateEdit.Properties.MaxValue = 100;
        _taxRateEdit.Properties.Increment = 0.5m;
        //
        // _taxInclusiveEdit
        //
        _taxInclusiveEdit.Name = "_taxInclusiveEdit";
        _taxInclusiveEdit.Text = "Tax-inclusive pricing";
        //
        // ProductEditForm
        //








        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_skuEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_categoryCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_groupCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_brandCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_unitCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_taxRateEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_taxInclusiveEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.TextEdit _nameEdit;
    private DevExpress.XtraEditors.TextEdit _skuEdit;
    private DevExpress.XtraEditors.ComboBoxEdit _categoryCombo;
    private DevExpress.XtraEditors.ComboBoxEdit _groupCombo;
    private DevExpress.XtraEditors.ComboBoxEdit _brandCombo;
    private DevExpress.XtraEditors.ComboBoxEdit _unitCombo;
    private DevExpress.XtraEditors.SpinEdit _taxRateEdit;
    private DevExpress.XtraEditors.CheckEdit _taxInclusiveEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.Label label7;
}
