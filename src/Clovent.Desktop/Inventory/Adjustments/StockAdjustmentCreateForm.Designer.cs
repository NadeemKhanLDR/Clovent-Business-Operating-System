using Clovent.Inventory.Adjustments;

namespace Clovent.Desktop.Inventory.Adjustments;

partial class StockAdjustmentCreateForm
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
        _variantCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _typeCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _quantityEdit = new DevExpress.XtraEditors.SpinEdit();
        _reasonEdit = new DevExpress.XtraEditors.MemoEdit();
        ((System.ComponentModel.ISupportInitialize)_variantCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_typeCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_quantityEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_reasonEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 4;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_variantCombo, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Variant:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _variantCombo
        _variantCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_typeCombo, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Type:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _typeCombo
        _typeCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_quantityEdit, 1, 2);
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "Quantity:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _quantityEdit
        _quantityEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label4, 0, 3);
        _contentPanel.Controls.Add(_reasonEdit, 1, 3);
        // label4
        label4.AutoSize = true;
        label4.Dock = System.Windows.Forms.DockStyle.Left;
        label4.Text = "Reason:";
        label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label4.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _reasonEdit
        _reasonEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _variantCombo
        //
        _variantCombo.Name = "_variantCombo";
        //
        // _typeCombo
        //
        _typeCombo.Name = "_typeCombo";
        _typeCombo.Properties.Items.AddRange(["Increase", "Decrease"]);
        _typeCombo.SelectedIndex = 0;
        //
        // _quantityEdit
        //
        _quantityEdit.Name = "_quantityEdit";
        _quantityEdit.Properties.MinValue = 0.0001m;
        _quantityEdit.Properties.MaxValue = 1_000_000;
        //
        // _reasonEdit
        //
        _reasonEdit.Name = "_reasonEdit";
        _reasonEdit.Height = 60;
        //
        // StockAdjustmentCreateForm
        //




        ((System.ComponentModel.ISupportInitialize)_variantCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_typeCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_quantityEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_reasonEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.ComboBoxEdit _variantCombo;
    private DevExpress.XtraEditors.ComboBoxEdit _typeCombo;
    private DevExpress.XtraEditors.SpinEdit _quantityEdit;
    private DevExpress.XtraEditors.MemoEdit _reasonEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label4;
}
