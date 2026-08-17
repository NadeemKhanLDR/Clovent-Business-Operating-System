namespace Clovent.Desktop.Inventory.WarehouseStocks;

partial class WarehouseStockEditForm
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
        _variantLabel = new System.Windows.Forms.Label();
        _variantCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        label2 = new System.Windows.Forms.Label();
        _minimumStockEdit = new DevExpress.XtraEditors.SpinEdit();
        _maximumStockEdit = new DevExpress.XtraEditors.SpinEdit();
        _allowNegativeStockEdit = new DevExpress.XtraEditors.CheckEdit();
        ((System.ComponentModel.ISupportInitialize)_minimumStockEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_maximumStockEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_allowNegativeStockEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 4;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_minimumStockEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Minimum Stock:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _minimumStockEdit
        _minimumStockEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_maximumStockEdit, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Maximum Stock:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _maximumStockEdit
        _maximumStockEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(_allowNegativeStockEdit, 0, 2);
        _contentPanel.SetColumnSpan(_allowNegativeStockEdit, 2);
        // _allowNegativeStockEdit
        _allowNegativeStockEdit.Dock = System.Windows.Forms.DockStyle.Top;
        
        // _variantLabel
        _variantLabel.AutoSize = true;
        _variantLabel.Dock = System.Windows.Forms.DockStyle.Left;
        _variantLabel.Text = "Variant:";
        _variantLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _variantLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        _variantLabel.Visible = false;

        // _variantCombo
        _variantCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _variantCombo.Visible = false;

        _contentPanel.Controls.Add(_variantLabel, 0, 3);
        _contentPanel.Controls.Add(_variantCombo, 1, 3);
_contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _minimumStockEdit
        //
        _minimumStockEdit.Name = "_minimumStockEdit";
        _minimumStockEdit.Properties.MinValue = 0;
        _minimumStockEdit.Properties.MaxValue = 1_000_000;
        //
        // _maximumStockEdit
        //
        _maximumStockEdit.Name = "_maximumStockEdit";
        _maximumStockEdit.Properties.MinValue = 0;
        _maximumStockEdit.Properties.MaxValue = 1_000_000;
        //
        // _allowNegativeStockEdit
        //
        _allowNegativeStockEdit.Name = "_allowNegativeStockEdit";
        _allowNegativeStockEdit.Text = "Allow negative stock";
        //
        // WarehouseStockEditForm
        //



        ((System.ComponentModel.ISupportInitialize)_minimumStockEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_maximumStockEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_allowNegativeStockEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.SpinEdit _minimumStockEdit;
    private DevExpress.XtraEditors.SpinEdit _maximumStockEdit;
    private DevExpress.XtraEditors.CheckEdit _allowNegativeStockEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label _variantLabel;
    private DevExpress.XtraEditors.ComboBoxEdit _variantCombo;
    private System.Windows.Forms.Label label2;
}
