namespace Clovent.Desktop.Notifications;

/// <summary><see cref="INotificationService"/> implementation - a process-wide singleton in-memory list.</summary>
public sealed class NotificationService : INotificationService
{
    private readonly List<Notification> _notifications = [];

    /// <inheritdoc/>
    public IReadOnlyList<Notification> Notifications => _notifications.AsReadOnly();

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public void Add(string title, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _notifications.Insert(0, new Notification(title, message, DateTimeOffset.UtcNow));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _notifications.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
