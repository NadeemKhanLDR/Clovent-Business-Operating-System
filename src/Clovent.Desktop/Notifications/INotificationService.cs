namespace Clovent.Desktop.Notifications;

/// <summary>The "Notifications" deliverable: an in-memory list of notifications the Shell's Ribbon surfaces (a count badge, an opened list).</summary>
public interface INotificationService
{
    /// <summary>Every notification currently held, most recent first.</summary>
    IReadOnlyList<Notification> Notifications { get; }

    /// <summary>Adds a notification.</summary>
    void Add(string title, string message);

    /// <summary>Removes every notification.</summary>
    void Clear();

    /// <summary>Raised whenever <see cref="Notifications"/> changes.</summary>
    event EventHandler? Changed;
}
