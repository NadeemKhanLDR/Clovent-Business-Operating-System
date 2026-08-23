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
        label1 = new LabelControl();
        _tableCombo = new ComboBoxEdit();
        ((System.ComponentModel.ISupportInitialize)_tableCombo.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 1;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_tableCombo, 1, 0);
        //
        // label1
        //
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "New Table:";
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        //
        // _tableCombo
        //
        _tableCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _tableCombo.Width = 260;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // TableTransferDialog
        //
        ClientSize = new System.Drawing.Size(480, 200);
        Name = "TableTransferDialog";
        ((System.ComponentModel.ISupportInitialize)_tableCombo.Properties).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ComboBoxEdit _tableCombo;

    private LabelControl label1;
}
