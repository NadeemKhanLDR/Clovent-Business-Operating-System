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
        _currentPasswordEdit = new DevExpress.XtraEditors.TextEdit();
        _newPasswordEdit = new DevExpress.XtraEditors.TextEdit();
        _confirmPasswordEdit = new DevExpress.XtraEditors.TextEdit();
        ((System.ComponentModel.ISupportInitialize)_currentPasswordEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_newPasswordEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_confirmPasswordEdit.Properties).BeginInit();
        SuspendLayout();
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
}
