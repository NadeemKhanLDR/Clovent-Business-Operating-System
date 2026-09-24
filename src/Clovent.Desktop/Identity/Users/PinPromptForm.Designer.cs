namespace Clovent.Desktop.Identity.Users;

partial class PinPromptForm
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
        _newPinEdit = new DevExpress.XtraEditors.TextEdit();
        _confirmPinEdit = new DevExpress.XtraEditors.TextEdit();
        ((System.ComponentModel.ISupportInitialize)_newPinEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_confirmPinEdit.Properties).BeginInit();
        SuspendLayout();
        //
        // _newPinEdit
        //
        _newPinEdit.Name = "_newPinEdit";
        _newPinEdit.Properties.PasswordChar = '*';
        //
        // _confirmPinEdit
        //
        _confirmPinEdit.Name = "_confirmPinEdit";
        _confirmPinEdit.Properties.PasswordChar = '*';
        //
        // PinPromptForm
        //
        ((System.ComponentModel.ISupportInitialize)_newPinEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_confirmPinEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.TextEdit _newPinEdit;
    private DevExpress.XtraEditors.TextEdit _confirmPinEdit;
}
