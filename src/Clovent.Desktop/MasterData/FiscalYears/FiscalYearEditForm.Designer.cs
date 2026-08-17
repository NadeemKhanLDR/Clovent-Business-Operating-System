namespace Clovent.Desktop.MasterData.FiscalYears;

partial class FiscalYearEditForm
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
        _nameEdit = new DevExpress.XtraEditors.TextEdit();
        _startDateEdit = new DevExpress.XtraEditors.DateEdit();
        _endDateEdit = new DevExpress.XtraEditors.DateEdit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_startDateEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_endDateEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 3;
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
        _contentPanel.Controls.Add(_startDateEdit, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Start Date:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _startDateEdit
        _startDateEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_endDateEdit, 1, 2);
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "End Date:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _endDateEdit
        _endDateEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _nameEdit
        //
        _nameEdit.Name = "_nameEdit";
        //
        // _startDateEdit
        //
        _startDateEdit.Name = "_startDateEdit";
        //
        // _endDateEdit
        //
        _endDateEdit.Name = "_endDateEdit";
        //
        // FiscalYearEditForm
        //



        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_startDateEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_endDateEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.TextEdit _nameEdit;
    private DevExpress.XtraEditors.DateEdit _startDateEdit;
    private DevExpress.XtraEditors.DateEdit _endDateEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
}
