using DevExpress.XtraEditors;

namespace Clovent.Desktop.Notifications;

/// <summary>A simple modal list of the Shell's current notifications. Built entirely in code - see <see cref="Shell.ShellForm"/>'s doc comment for why.</summary>
public sealed class NotificationsForm : XtraForm
{
    /// <summary>Builds the list, showing a placeholder row when <paramref name="notifications"/> is empty.</summary>
    public NotificationsForm(IReadOnlyList<Notification> notifications)
    {
        Text = "Notifications";
        StartPosition = FormStartPosition.CenterParent;
        Width = 420;
        Height = 360;
        MinimizeBox = false;
        MaximizeBox = false;

        var list = new ListBoxControl { Dock = DockStyle.Fill };
        list.Items.AddRange([.. notifications.Select(n => $"{n.TimestampUtc:g}  -  {n.Title}: {n.Message}")]);
        if (notifications.Count == 0)
        {
            list.Items.Add("No notifications.");
        }

        Controls.Add(list);
    }
}
