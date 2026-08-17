namespace Clovent.Desktop.Identity.Users;

partial class PasswordPromptForm
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
        _currentPasswordEdit = new DevExpress.XtraEditors.TextEdit();
        _newPasswordEdit = new DevExpress.XtraEditors.TextEdit();
        _confirmPasswordEdit = new DevExpress.XtraEditors.TextEdit();
        ((System.ComponentModel.ISupportInitialize)_currentPasswordEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_newPasswordEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_confirmPasswordEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 3;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_currentPasswordEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Current Password:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _currentPasswordEdit
        _currentPasswordEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_newPasswordEdit, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "New Password:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _newPasswordEdit
        _newPasswordEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_confirmPasswordEdit, 1, 2);
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "Confirm Password:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _confirmPasswordEdit
        _confirmPasswordEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _currentPasswordEdit
        //
        _currentPasswordEdit.Name = "_currentPasswordEdit";
        _currentPasswordEdit.Properties.PasswordChar = '*';
        //
        // _newPasswordEdit
        //
        _newPasswordEdit.Name = "_newPasswordEdit";
        _newPasswordEdit.Properties.PasswordChar = '*';
        //
        // _confirmPasswordEdit
        //
        _confirmPasswordEdit.Name = "_confirmPasswordEdit";
        _confirmPasswordEdit.Properties.PasswordChar = '*';
        //
        // PasswordPromptForm
        //
        ((System.ComponentModel.ISupportInitialize)_currentPasswordEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_newPasswordEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_confirmPasswordEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.TextEdit _currentPasswordEdit;
    private DevExpress.XtraEditors.TextEdit _newPasswordEdit;
    private DevExpress.XtraEditors.TextEdit _confirmPasswordEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
}
