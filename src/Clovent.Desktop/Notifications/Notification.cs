namespace Clovent.Desktop.Notifications;

/// <summary>A single in-app notification shown in the Shell's notification list.</summary>
public sealed record Notification(string Title, string Message, DateTimeOffset TimestampUtc);
