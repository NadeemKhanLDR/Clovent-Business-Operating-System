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
            DesktopDialogSizing.Apply(this, 560, 420, 440, 320, owner: Owner, resizable: true);
            _notificationsList.ItemHeight = DesktopDpi.Scale(36, this);
            _closeButton.Size = new Size(DesktopDpi.Scale(100, this), DesktopDpi.Scale(32, this));
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
