using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

partial class TableTransferDialog
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
        _tableCombo = new ComboBoxEdit();
        ((System.ComponentModel.ISupportInitialize)_tableCombo.Properties).BeginInit();
        SuspendLayout();
        // 
        // _tableCombo
        // 
        _tableCombo.Location = new Point(0, 0);
        _tableCombo.Name = "_tableCombo";
        _tableCombo.Size = new Size(250, 50);
        _tableCombo.TabIndex = 0;
        // 
        // TableTransferDialog
        // 
        ClientSize = new Size(476, 314);
        MinimumSize = new Size(480, 360);
        Name = "TableTransferDialog";
        ((System.ComponentModel.ISupportInitialize)_tableCombo.Properties).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ComboBoxEdit _tableCombo;
}
