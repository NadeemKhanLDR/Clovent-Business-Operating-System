using DevExpress.XtraEditors;

namespace Clovent.Desktop.Notifications;

/// <summary>A simple modal list of the Shell's current notifications. Built entirely in code - see <see cref="Clovent.Desktop.Forms.Shell.MainForm"/>'s doc comment for why.</summary>
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

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _notificationsList.Items.AddRange([.. notifications.Select(n => $"{n.TimestampUtc:g}  -  {n.Title}: {n.Message}")]);
        if (notifications.Count == 0)
        {
            _notificationsList.Items.Add("No notifications.");
        }
    }
}
