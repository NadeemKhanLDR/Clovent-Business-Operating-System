namespace Clovent.Desktop.MasterData.Currencies;

partial class CurrencyCreateForm
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
        _codeEdit = new DevExpress.XtraEditors.TextEdit();
        _nameEdit = new DevExpress.XtraEditors.TextEdit();
        _symbolEdit = new DevExpress.XtraEditors.TextEdit();
        _decimalPlacesEdit = new DevExpress.XtraEditors.SpinEdit();
        ((System.ComponentModel.ISupportInitialize)_codeEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_symbolEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_decimalPlacesEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 4;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_codeEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Code (e.g. USD):";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _codeEdit
        _codeEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_nameEdit, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Name:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _nameEdit
        _nameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_symbolEdit, 1, 2);
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "Symbol:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _symbolEdit
        _symbolEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label4, 0, 3);
        _contentPanel.Controls.Add(_decimalPlacesEdit, 1, 3);
        // label4
        label4.AutoSize = true;
        label4.Dock = System.Windows.Forms.DockStyle.Left;
        label4.Text = "Decimal Places:";
        label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label4.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _decimalPlacesEdit
        _decimalPlacesEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _codeEdit
        //
        _codeEdit.Name = "_codeEdit";
        //
        // _nameEdit
        //
        _nameEdit.Name = "_nameEdit";
        //
        // _symbolEdit
        //
        _symbolEdit.Name = "_symbolEdit";
        //
        // _decimalPlacesEdit
        //
        _decimalPlacesEdit.Name = "_decimalPlacesEdit";
        _decimalPlacesEdit.Properties.MinValue = 0;
        _decimalPlacesEdit.Properties.MaxValue = 4;
        _decimalPlacesEdit.Value = 2;
        //
        // CurrencyCreateForm
        //




        ((System.ComponentModel.ISupportInitialize)_codeEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_symbolEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_decimalPlacesEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.TextEdit _codeEdit;
    private DevExpress.XtraEditors.TextEdit _nameEdit;
    private DevExpress.XtraEditors.TextEdit _symbolEdit;
    private DevExpress.XtraEditors.SpinEdit _decimalPlacesEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label4;
}
