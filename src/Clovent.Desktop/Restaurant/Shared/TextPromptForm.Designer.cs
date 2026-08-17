namespace Clovent.Desktop.Restaurant.Shared;

partial class TextPromptForm
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
        _textEdit = new DevExpress.XtraEditors.MemoEdit();
        ((System.ComponentModel.ISupportInitialize)_textEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 1;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_textEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _textEdit
        _textEdit.Height = 90;
        _textEdit.Dock = System.Windows.Forms.DockStyle.Fill;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _textEdit
        //
        _textEdit.Name = "_textEdit";
        _textEdit.Height = 80;
        //
        // TextPromptForm
        //
        ((System.ComponentModel.ISupportInitialize)_textEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.MemoEdit _textEdit;

    private System.Windows.Forms.Label label1;
}
