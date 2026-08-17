using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Tables;

partial class TableEditForm
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
        _codeEdit = new TextEdit();
        _capacityEdit = new SpinEdit();
        ((System.ComponentModel.ISupportInitialize)_codeEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_capacityEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 2;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_codeEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Code:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _codeEdit
        _codeEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_capacityEdit, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Capacity:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _capacityEdit
        _capacityEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _codeEdit
        //
        _codeEdit.Name = "_codeEdit";
        //
        // _capacityEdit
        //
        _capacityEdit.Name = "_capacityEdit";
        _capacityEdit.Properties.MinValue = 1;
        _capacityEdit.Properties.MaxValue = 100;
        //
        // TableEditForm
        //


        ((System.ComponentModel.ISupportInitialize)_codeEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_capacityEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private TextEdit _codeEdit;
    private SpinEdit _capacityEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
}
