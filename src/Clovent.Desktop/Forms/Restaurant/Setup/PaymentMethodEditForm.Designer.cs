using DevExpress.XtraEditors;

namespace Clovent.Desktop.Forms.Restaurant.Setup;

partial class PaymentMethodEditForm
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
        _nameEdit = new TextEdit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 1;
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
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _nameEdit
        //
        _nameEdit.Name = "_nameEdit";
        //
        // PaymentMethodEditForm
        //

        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private TextEdit _nameEdit;

    private System.Windows.Forms.Label label1;
}
