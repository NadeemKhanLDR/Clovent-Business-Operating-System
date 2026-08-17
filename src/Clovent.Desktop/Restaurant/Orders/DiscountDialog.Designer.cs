using Clovent.Restaurant.Discounts;

namespace Clovent.Desktop.Restaurant.Orders;

partial class DiscountDialog
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
        _typeCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _valueEdit = new DevExpress.XtraEditors.SpinEdit();
        _reasonEdit = new DevExpress.XtraEditors.MemoEdit();
        ((System.ComponentModel.ISupportInitialize)_typeCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_valueEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_reasonEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 3;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_typeCombo, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Type:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _typeCombo
        _typeCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_valueEdit, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Value:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _valueEdit
        _valueEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_reasonEdit, 1, 2);
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "Reason:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _reasonEdit
        _reasonEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _typeCombo
        //
        _typeCombo.Name = "_typeCombo";
        _typeCombo.Properties.Items.AddRange(["Percentage", "FixedAmount"]);
        _typeCombo.SelectedIndex = 0;
        //
        // _valueEdit
        //
        _valueEdit.Name = "_valueEdit";
        _valueEdit.Properties.MinValue = 0;
        _valueEdit.Properties.MaxValue = 1_000_000;
        _valueEdit.Properties.Increment = 0.01m;
        //
        // _reasonEdit
        //
        _reasonEdit.Height = 60;
        _reasonEdit.Name = "_reasonEdit";
        //
        // DiscountDialog
        //



        ((System.ComponentModel.ISupportInitialize)_typeCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_valueEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_reasonEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.ComboBoxEdit _typeCombo;
    private DevExpress.XtraEditors.SpinEdit _valueEdit;
    private DevExpress.XtraEditors.MemoEdit _reasonEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
}
