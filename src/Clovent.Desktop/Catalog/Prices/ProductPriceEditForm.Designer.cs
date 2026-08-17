using Clovent.Catalog.Prices;

namespace Clovent.Desktop.Catalog.Prices;

partial class ProductPriceEditForm
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
        _priceTypeCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _currencyCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _amountEdit = new DevExpress.XtraEditors.SpinEdit();
        ((System.ComponentModel.ISupportInitialize)_priceTypeCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_currencyCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_amountEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 3;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_priceTypeCombo, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Price Type:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _priceTypeCombo
        _priceTypeCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_currencyCombo, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Currency:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _currencyCombo
        _currencyCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_amountEdit, 1, 2);
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "Amount:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _amountEdit
        _amountEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _priceTypeCombo
        //
        _priceTypeCombo.Name = "_priceTypeCombo";
        _priceTypeCombo.Properties.Items.AddRange(["Cost", "Selling"]);
        //
        // _currencyCombo
        //
        _currencyCombo.Name = "_currencyCombo";
        //
        // _amountEdit
        //
        _amountEdit.Name = "_amountEdit";
        _amountEdit.Properties.MinValue = 0;
        _amountEdit.Properties.MaxValue = 1_000_000;
        _amountEdit.Properties.Increment = 0.01m;
        //
        // ProductPriceEditForm
        //



        ((System.ComponentModel.ISupportInitialize)_priceTypeCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_currencyCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_amountEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.ComboBoxEdit _priceTypeCombo;
    private DevExpress.XtraEditors.ComboBoxEdit _currencyCombo;
    private DevExpress.XtraEditors.SpinEdit _amountEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
}
