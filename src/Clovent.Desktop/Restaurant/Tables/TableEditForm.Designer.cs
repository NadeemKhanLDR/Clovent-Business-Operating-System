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
        labelName = new System.Windows.Forms.Label();
        label2 = new System.Windows.Forms.Label();
        _codeEdit = new TextEdit();
        _nameEdit = new TextEdit();
        _capacityEdit = new SpinEdit();
        ((System.ComponentModel.ISupportInitialize)_codeEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_capacityEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 4;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_codeEdit, 1, 0);
        _contentPanel.Controls.Add(labelName, 0, 1);
        _contentPanel.Controls.Add(_nameEdit, 1, 1);
        _contentPanel.Controls.Add(label2, 0, 2);
        _contentPanel.Controls.Add(_capacityEdit, 1, 2);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Code:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _codeEdit
        _codeEdit.Dock = System.Windows.Forms.DockStyle.Top;
        // labelName
        labelName.AutoSize = true;
        labelName.Dock = System.Windows.Forms.DockStyle.Left;
        labelName.Text = "Table Name *:";
        labelName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        labelName.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _nameEdit
        _nameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Capacity *:";
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
        // _nameEdit
        //
        _nameEdit.Name = "_nameEdit";
        //
        // _capacityEdit
        //
        _capacityEdit.Name = "_capacityEdit";
        _capacityEdit.Properties.IsFloatValue = false;
        _capacityEdit.Properties.Mask.EditMask = "N0";
        _capacityEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
        _capacityEdit.Properties.MinValue = 1;
        _capacityEdit.Properties.MaxValue = 100;
        //
        // TableEditForm
        //


        ((System.ComponentModel.ISupportInitialize)_codeEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_capacityEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private TextEdit _codeEdit;
    private TextEdit _nameEdit;
    private SpinEdit _capacityEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label labelName;
    private System.Windows.Forms.Label label2;
}
