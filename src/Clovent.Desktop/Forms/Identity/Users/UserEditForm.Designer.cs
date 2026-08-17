namespace Clovent.Desktop.Forms.Identity.Users;

partial class UserEditForm
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
        label4 = new System.Windows.Forms.Label();
        label5 = new System.Windows.Forms.Label();
        label6 = new System.Windows.Forms.Label();
        _emailEdit = new DevExpress.XtraEditors.TextEdit();
        _userNameEdit = new DevExpress.XtraEditors.TextEdit();
        _displayNameEdit = new DevExpress.XtraEditors.TextEdit();
        _companyCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _branchCombo = new DevExpress.XtraEditors.ComboBoxEdit();
        _rolesList = new DevExpress.XtraEditors.CheckedListBoxControl();
        ((System.ComponentModel.ISupportInitialize)_emailEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_userNameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_displayNameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_companyCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_branchCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_rolesList).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 6;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_emailEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Email:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _emailEdit
        _emailEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_userNameEdit, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Username:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _userNameEdit
        _userNameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_displayNameEdit, 1, 2);
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "Display Name:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _displayNameEdit
        _displayNameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label4, 0, 3);
        _contentPanel.Controls.Add(_companyCombo, 1, 3);
        // label4
        label4.AutoSize = true;
        label4.Dock = System.Windows.Forms.DockStyle.Left;
        label4.Text = "Company:";
        label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label4.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _companyCombo
        _companyCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label5, 0, 4);
        _contentPanel.Controls.Add(_branchCombo, 1, 4);
        // label5
        label5.AutoSize = true;
        label5.Dock = System.Windows.Forms.DockStyle.Left;
        label5.Text = "Branch:";
        label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label5.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _branchCombo
        _branchCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label6, 0, 5);
        _contentPanel.Controls.Add(_rolesList, 1, 5);
        // label6
        label6.AutoSize = true;
        label6.Dock = System.Windows.Forms.DockStyle.Left;
        label6.Text = "Roles:";
        label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label6.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _rolesList
        _rolesList.Height = 100;
        _rolesList.Dock = System.Windows.Forms.DockStyle.Fill;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _emailEdit
        //
        _emailEdit.Name = "_emailEdit";
        //
        // _userNameEdit
        //
        _userNameEdit.Name = "_userNameEdit";
        //
        // _displayNameEdit
        //
        _displayNameEdit.Name = "_displayNameEdit";
        //
        // _companyCombo
        //
        _companyCombo.Name = "_companyCombo";
        _companyCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        //
        // _branchCombo
        //
        _branchCombo.Name = "_branchCombo";
        _branchCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        //
        // _rolesList
        //
        _rolesList.CheckOnClick = true;
        _rolesList.Height = 90;
        _rolesList.Name = "_rolesList";
        //
        // UserEditForm
        //
        Height = 620;






        ((System.ComponentModel.ISupportInitialize)_emailEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_userNameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_displayNameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_companyCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_branchCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_rolesList).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.TextEdit _emailEdit;
    private DevExpress.XtraEditors.TextEdit _userNameEdit;
    private DevExpress.XtraEditors.TextEdit _displayNameEdit;
    private DevExpress.XtraEditors.ComboBoxEdit _companyCombo;
    private DevExpress.XtraEditors.ComboBoxEdit _branchCombo;
    private DevExpress.XtraEditors.CheckedListBoxControl _rolesList;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label label6;
}
