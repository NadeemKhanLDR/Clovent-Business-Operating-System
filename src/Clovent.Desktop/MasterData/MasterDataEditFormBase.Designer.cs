using System.Windows.Forms;
using System.Drawing;

namespace Clovent.Desktop.MasterData;

partial class MasterDataEditFormBase
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
        _contentPanel = new TableLayoutPanel();
        _buttonPanel = new FlowLayoutPanel();
        _cancelButton = new DevExpress.XtraEditors.SimpleButton();
        _okButton = new DevExpress.XtraEditors.SimpleButton();
        _saveAndNewButton = new DevExpress.XtraEditors.SimpleButton();
        _buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // _contentPanel
        //
        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.ColumnCount = 2;
        _contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _contentPanel.AutoSize = true;
        _contentPanel.Padding = new Padding(12);
        _contentPanel.Name = "_contentPanel";
        //
        // _buttonPanel
        //
        _buttonPanel.Controls.Add(_cancelButton);
        _buttonPanel.Controls.Add(_okButton);
        _buttonPanel.Dock = DockStyle.Bottom;
        _buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        _buttonPanel.Height = 48;
        _buttonPanel.Name = "_buttonPanel";
        //
        // _cancelButton
        //
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Text = "Cancel";
        //
        // _okButton
        //
        _okButton.Name = "_okButton";
        _okButton.Text = "OK";
        _okButton.Click += OkButton_Click;
        //
        // _saveAndNewButton
        //
        // Not added to _buttonPanel here - only screens that call
        // EnableSaveAndNew (MasterDataEditFormBase.cs) opt into showing it.
        _saveAndNewButton.Name = "_saveAndNewButton";
        _saveAndNewButton.Text = "Save && New";
        _saveAndNewButton.Click += SaveAndNewButton_Click;
        //
        // MasterDataEditFormBase
        //
        Controls.Add(_contentPanel);
        Controls.Add(_buttonPanel);
        AcceptButton = _okButton;
        CancelButton = _cancelButton;
        Width = 480;
        Height = 360;
        StartPosition = FormStartPosition.CenterParent;
        // Resizable/maximizable (was FixedDialog/MaximizeBox=false) - "every
        // popup window in the Restaurant module must be resizable, support
        // maximize, remember its last size/position" applies to every
        // *EditForm/*PromptForm built on this shared base (~27+ dialogs
        // across every module, not just Restaurant's own), so the fix
        // belongs here once rather than being repeated per subclass.
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Name = "MasterDataEditFormBase";
        Load += MasterDataEditFormBase_Load;
        FormClosed += MasterDataEditFormBase_FormClosed;
        _buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    protected TableLayoutPanel _contentPanel;
    private DevExpress.XtraEditors.SimpleButton _okButton;
    private DevExpress.XtraEditors.SimpleButton _saveAndNewButton;
    private DevExpress.XtraEditors.SimpleButton _cancelButton;
    private FlowLayoutPanel _buttonPanel;
}
