using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Notifications;

/// <summary>A simple modal list of the Shell's current notifications.</summary>
public sealed partial class NotificationsForm : XtraForm
{
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public NotificationsForm()
    {
        InitializeComponent();
    }

    /// <summary>Builds the list, showing a placeholder row when <paramref name="notifications"/> is empty.</summary>
    public NotificationsForm(IReadOnlyList<Notification> notifications) : base()
    {
        InitializeComponent();

        if (DesignModeHelper.IsInDesignMode)
            return;

        Load += (s, e) =>
        {
            DesktopDialogSizing.Apply(this, 560, 440, 460, 340, owner: Owner, resizable: true);
            _headerPanel.Height = DesktopDpi.Scale(72, this);
            _headerPanel.Padding = new Padding(
                DesktopDpi.Scale(16, this),
                DesktopDpi.Scale(14, this),
                DesktopDpi.Scale(16, this),
                DesktopDpi.Scale(8, this));
            _footerPanel.Height = DesktopDpi.Scale(58, this);
            _footerPanel.Padding = new Padding(
                DesktopDpi.Scale(16, this),
                DesktopDpi.Scale(10, this),
                DesktopDpi.Scale(16, this),
                DesktopDpi.Scale(12, this));
            _notificationsList.ItemHeight = DesktopDpi.Scale(36, this);
            var btnWidth = DesktopDpi.Scale(120, this);
            var btnHeight = DesktopDpi.Scale(36, this);
            _closeButton.Size = new Size(btnWidth, btnHeight);
            _closeButton.MinimumSize = new Size(DesktopDpi.Scale(110, this), DesktopDpi.Scale(34, this));
            Clovent.Desktop.Forms.Base.Localization.LocalizationHelper.LocalizeControl(this);
        };

        AcceptButton = _closeButton;
        CancelButton = _closeButton;

        _notificationsList.Items.AddRange([.. notifications.Select(n => $"{n.TimestampUtc:g}  -  {n.Title}: {n.Message}")]);
        if (notifications.Count == 0)
        {
            _notificationsList.Items.Add("No notifications.");
        }
    }
}
