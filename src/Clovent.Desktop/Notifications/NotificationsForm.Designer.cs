namespace Clovent.Desktop.Notifications;

partial class NotificationsForm
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
        _notificationsList = new DevExpress.XtraEditors.ListBoxControl();
        ((System.ComponentModel.ISupportInitialize)_notificationsList).BeginInit();
        SuspendLayout();
        //
        // _notificationsList
        //
        _notificationsList.Dock = DockStyle.Fill;
        _notificationsList.Name = "_notificationsList";
        //
        // NotificationsForm
        //
        Text = "Notifications";
        StartPosition = FormStartPosition.CenterParent;
        Width = 420;
        Height = 360;
        MinimizeBox = false;
        MaximizeBox = false;
        Controls.Add(_notificationsList);
        ((System.ComponentModel.ISupportInitialize)_notificationsList).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.ListBoxControl _notificationsList;
}
