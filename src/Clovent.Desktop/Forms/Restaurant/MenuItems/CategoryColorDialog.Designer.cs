using DevExpress.XtraEditors;

namespace Clovent.Desktop.Forms.Restaurant.MenuItems;

partial class CategoryColorDialog
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
        labelCategory = new System.Windows.Forms.Label();
        _categoryNameEdit = new TextEdit();
        label1 = new System.Windows.Forms.Label();
        _colorEdit = new ColorEdit();
        _clearCheck = new CheckEdit();
        ((System.ComponentModel.ISupportInitialize)_categoryNameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_colorEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_clearCheck.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 3;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        
        // Row 0: Category
        _contentPanel.Controls.Add(labelCategory, 0, 0);
        _contentPanel.Controls.Add(_categoryNameEdit, 1, 0);
        
        // Row 1: Color
        _contentPanel.Controls.Add(label1, 0, 1);
        _contentPanel.Controls.Add(_colorEdit, 1, 1);
        
        // Row 2: Clear
        _contentPanel.Controls.Add(_clearCheck, 0, 2);
        _contentPanel.SetColumnSpan(_clearCheck, 2);

        // labelCategory
        labelCategory.AutoSize = true;
        labelCategory.Dock = System.Windows.Forms.DockStyle.Left;
        labelCategory.Text = "Category:";
        labelCategory.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        labelCategory.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);

        // _categoryNameEdit
        _categoryNameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _categoryNameEdit.Name = "_categoryNameEdit";
        _categoryNameEdit.Properties.ReadOnly = true;
        _categoryNameEdit.Width = 260;

        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Color:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);

        // _colorEdit
        _colorEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _colorEdit.Name = "_colorEdit";
        _colorEdit.Width = 260;

        // _clearCheck
        _clearCheck.Dock = System.Windows.Forms.DockStyle.Top;
        _clearCheck.Name = "_clearCheck";
        _clearCheck.Text = "Clear color (use default)";
        _clearCheck.CheckedChanged += ClearCheck_CheckedChanged;

        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();

        ((System.ComponentModel.ISupportInitialize)_categoryNameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_colorEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_clearCheck.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private TextEdit _categoryNameEdit;
    private ColorEdit _colorEdit;
    private CheckEdit _clearCheck;

    private System.Windows.Forms.Label labelCategory;
    private System.Windows.Forms.Label label1;
}
