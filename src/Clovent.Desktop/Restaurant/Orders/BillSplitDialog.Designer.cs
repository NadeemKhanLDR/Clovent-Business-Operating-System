namespace Clovent.Desktop.Restaurant.Orders;

partial class BillSplitDialog
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
        _linesList = new DevExpress.XtraEditors.CheckedListBoxControl();
        _targetTableCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        ((System.ComponentModel.ISupportInitialize)_linesList).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_targetTableCombo.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 2;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_linesList, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Lines to Move:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _linesList
        _linesList.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_targetTableCombo, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Target Table:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _targetTableCombo
        _targetTableCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _linesList
        //
        _linesList.Name = "_linesList";
        //
        // _targetTableCombo
        //
        _targetTableCombo.Name = "_targetTableCombo";
        //
        // BillSplitDialog
        //
        Height = 440;

        _linesList.Width = 320;
        _linesList.Height = 200;

        ((System.ComponentModel.ISupportInitialize)_linesList).EndInit();
        ((System.ComponentModel.ISupportInitialize)_targetTableCombo.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.CheckedListBoxControl _linesList;
    private DevExpress.XtraEditors.ComboBoxEdit _targetTableCombo;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
}
