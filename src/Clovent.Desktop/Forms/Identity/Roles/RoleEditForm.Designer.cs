namespace Clovent.Desktop.Forms.Identity.Roles;

partial class RoleEditForm
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
    /// this method with the code editor. Uses the inherited
    /// <c>AddField(label, editor)</c> from <see cref="MasterData.MasterDataEditFormBase"/>
    /// for layout, the same shared chrome every other master-data edit
    /// dialog uses.
    /// </summary>
    private void InitializeComponent()
    {
        label1 = new System.Windows.Forms.Label();
        label2 = new System.Windows.Forms.Label();
        _nameEdit = new DevExpress.XtraEditors.TextEdit();
        _permissionsList = new DevExpress.XtraEditors.CheckedListBoxControl();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_permissionsList).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 2;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
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
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_permissionsList, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Permissions:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _permissionsList
        _permissionsList.Height = 230;
        _permissionsList.Dock = System.Windows.Forms.DockStyle.Fill;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _nameEdit
        //
        _nameEdit.Name = "_nameEdit";
        //
        // _permissionsList
        //
        // Wider than the 480px dialog default - permission codes like
        // "feature.warehousestocks.receive" were clipped to ~8 characters
        // ("menu.st...") at the default width, confirmed via manual
        // verification.
        _permissionsList.CheckOnClick = true;
        _permissionsList.Height = 220;
        _permissionsList.Name = "_permissionsList";
        //
        // RoleEditForm
        //
        Width = 640;
        Height = 560;


        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_permissionsList).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.TextEdit _nameEdit;
    private DevExpress.XtraEditors.CheckedListBoxControl _permissionsList;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
}
