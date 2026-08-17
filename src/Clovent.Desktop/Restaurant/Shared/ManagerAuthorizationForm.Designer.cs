namespace Clovent.Desktop.Restaurant.Shared;

partial class ManagerAuthorizationForm
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
        _headerLabel = new DevExpress.XtraEditors.LabelControl();
        _detailLabel = new DevExpress.XtraEditors.LabelControl();
        _userNameEdit = new DevExpress.XtraEditors.TextEdit();
        _passwordEdit = new DevExpress.XtraEditors.TextEdit();
        ((System.ComponentModel.ISupportInitialize)_userNameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_passwordEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 2;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_userNameEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Manager Username:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _userNameEdit
        _userNameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_passwordEdit, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Manager Password:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _passwordEdit
        _passwordEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _headerLabel
        //
        _headerLabel.Name = "_headerLabel";
        _headerLabel.Dock = System.Windows.Forms.DockStyle.Top;
        _headerLabel.Text = "Manager authorization required";
        _headerLabel.Height = 26;
        _headerLabel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 0);
        _headerLabel.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        _headerLabel.Appearance.Options.UseFont = true;
        //
        // _detailLabel
        //
        _detailLabel.Name = "_detailLabel";
        _detailLabel.Dock = System.Windows.Forms.DockStyle.Top;
        _detailLabel.Padding = new System.Windows.Forms.Padding(12, 4, 12, 8);
        _detailLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
        _detailLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        _detailLabel.Appearance.Options.UseTextOptions = true;
        //
        // _userNameEdit
        //
        _userNameEdit.Name = "_userNameEdit";
        //
        // _passwordEdit
        //
        _passwordEdit.Name = "_passwordEdit";
        _passwordEdit.Properties.UseSystemPasswordChar = true;
        //
        // ManagerAuthorizationForm
        //
        Controls.Add(_detailLabel);
        Controls.Add(_headerLabel);
        ((System.ComponentModel.ISupportInitialize)_userNameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_passwordEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.LabelControl _headerLabel;
    private DevExpress.XtraEditors.LabelControl _detailLabel;
    private DevExpress.XtraEditors.TextEdit _userNameEdit;
    private DevExpress.XtraEditors.TextEdit _passwordEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
}
