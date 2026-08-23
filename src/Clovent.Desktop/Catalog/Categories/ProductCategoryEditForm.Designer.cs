namespace Clovent.Desktop.Catalog.Categories;

partial class ProductCategoryEditForm
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
        _parentCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _colorEdit = new DevExpress.XtraEditors.ColorEdit();
        _clearColorCheck = new DevExpress.XtraEditors.CheckEdit { Text = "Clear Color / Use Default", Checked = true };
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_parentCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_colorEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_clearColorCheck.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 4;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
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
        _nameEdit.Width = 260;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_parentCombo, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Parent Category:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _parentCombo
        _parentCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _parentCombo.Width = 260;
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "Color:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        _contentPanel.Controls.Add(label3, 0, 2);
        // _colorEdit
        _colorEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _colorEdit.Width = 260;
        _contentPanel.Controls.Add(_colorEdit, 1, 2);
        // _clearColorCheck
        _clearColorCheck.Dock = System.Windows.Forms.DockStyle.Top;
        _clearColorCheck.Width = 260;
        _contentPanel.Controls.Add(_clearColorCheck, 1, 3);
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _nameEdit
        //
        _nameEdit.Name = "_nameEdit";
        //
        // _parentCombo
        //
        _parentCombo.Name = "_parentCombo";
        //
        // _colorEdit
        //
        _colorEdit.Name = "_colorEdit";
        //
        // _clearColorCheck
        //
        _clearColorCheck.Name = "_clearColorCheck";
        //
        // ProductCategoryEditForm
        //


        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_parentCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_colorEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_clearColorCheck.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.TextEdit _nameEdit;
    private DevExpress.XtraEditors.ComboBoxEdit _parentCombo;
    private DevExpress.XtraEditors.ColorEdit _colorEdit;
    private DevExpress.XtraEditors.CheckEdit _clearColorCheck;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
}
