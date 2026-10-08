using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;

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
        _headerPanel = new DevExpress.XtraEditors.PanelControl();
        _titleLabel = new DevExpress.XtraEditors.LabelControl();
        _subtitleLabel = new DevExpress.XtraEditors.LabelControl();
        _contentPanel = new DevExpress.XtraEditors.PanelControl();
        _notificationsList = new DevExpress.XtraEditors.ListBoxControl();
        _footerPanel = new DevExpress.XtraEditors.PanelControl();
        _closeButton = new DevExpress.XtraEditors.SimpleButton();
        ((System.ComponentModel.ISupportInitialize)_headerPanel).BeginInit();
        _headerPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_contentPanel).BeginInit();
        _contentPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_notificationsList).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_footerPanel).BeginInit();
        _footerPanel.SuspendLayout();
        SuspendLayout();
        //
        // _headerPanel
        //
        _headerPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _headerPanel.Controls.Add(_subtitleLabel);
        _headerPanel.Controls.Add(_titleLabel);
        _headerPanel.Dock = DockStyle.Top;
        _headerPanel.Height = 56;
        _headerPanel.Padding = new Padding(16, 12, 16, 6);
        _headerPanel.Name = "_headerPanel";
        //
        // _titleLabel
        //
        _titleLabel.Appearance.Font = DesktopStyle.SectionHeadingFont;
        _titleLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _titleLabel.Appearance.Options.UseFont = true;
        _titleLabel.Appearance.Options.UseForeColor = true;
        _titleLabel.Dock = DockStyle.Top;
        _titleLabel.Name = "_titleLabel";
        _titleLabel.Text = "NOTIFICATIONS";
        //
        // _subtitleLabel
        //
        _subtitleLabel.Appearance.Font = new Font("Segoe UI", 9F);
        _subtitleLabel.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        _subtitleLabel.Appearance.Options.UseFont = true;
        _subtitleLabel.Appearance.Options.UseForeColor = true;
        _subtitleLabel.Dock = DockStyle.Bottom;
        _subtitleLabel.Name = "_subtitleLabel";
        _subtitleLabel.Text = "Recent system alerts and operational messages.";
        //
        // _contentPanel
        //
        _contentPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _contentPanel.Controls.Add(_notificationsList);
        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.Padding = new Padding(16, 4, 16, 6);
        _contentPanel.Name = "_contentPanel";
        //
        // _notificationsList
        //
        _notificationsList.Appearance.Font = new Font("Segoe UI", 9.5F);
        _notificationsList.Appearance.Options.UseFont = true;
        _notificationsList.Dock = DockStyle.Fill;
        _notificationsList.ItemHeight = 34;
        _notificationsList.Name = "_notificationsList";
        //
        // _footerPanel
        //
        _footerPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _footerPanel.Controls.Add(_closeButton);
        _footerPanel.Dock = DockStyle.Bottom;
        _footerPanel.Height = 50;
        _footerPanel.Padding = new Padding(16, 8, 16, 10);
        _footerPanel.Name = "_footerPanel";
        //
        // _closeButton
        //
        _closeButton.Appearance.Font = new Font("Segoe UI", 9.5F);
        _closeButton.Appearance.Options.UseFont = true;
        _closeButton.DialogResult = DialogResult.OK;
        _closeButton.Dock = DockStyle.Right;
        _closeButton.Name = "_closeButton";
        _closeButton.Size = new Size(100, 32);
        _closeButton.Text = "Close";
        //
        // NotificationsForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(560, 420);
        Controls.Add(_contentPanel);
        Controls.Add(_footerPanel);
        Controls.Add(_headerPanel);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        MinimumSize = new Size(440, 320);
        Name = "NotificationsForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Notifications";
        ((System.ComponentModel.ISupportInitialize)_headerPanel).EndInit();
        _headerPanel.ResumeLayout(false);
        _headerPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_contentPanel).EndInit();
        _contentPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_notificationsList).EndInit();
        ((System.ComponentModel.ISupportInitialize)_footerPanel).EndInit();
        _footerPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.PanelControl _headerPanel;
    private DevExpress.XtraEditors.LabelControl _titleLabel;
    private DevExpress.XtraEditors.LabelControl _subtitleLabel;
    private DevExpress.XtraEditors.PanelControl _contentPanel;
    private DevExpress.XtraEditors.ListBoxControl _notificationsList;
    private DevExpress.XtraEditors.PanelControl _footerPanel;
    private DevExpress.XtraEditors.SimpleButton _closeButton;
}
