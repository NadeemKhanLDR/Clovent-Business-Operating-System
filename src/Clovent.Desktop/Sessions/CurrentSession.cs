namespace Clovent.Desktop.Sessions;

/// <summary>Default, in-memory <see cref="ICurrentSession"/> - a process-wide singleton, since a desktop app has exactly one signed-in user at a time.</summary>
public sealed class CurrentSession : ICurrentSession
{
    /// <inheritdoc/>
    public bool IsAuthenticated { get; private set; }

    /// <inheritdoc/>
    public Guid? UserId { get; private set; }

    /// <inheritdoc/>
    public Guid? SessionId { get; private set; }

    /// <inheritdoc/>
    public string? DisplayName { get; private set; }

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public void SignIn(Guid userId, Guid sessionId, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        UserId = userId;
        SessionId = sessionId;
        DisplayName = displayName;
        IsAuthenticated = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void SignOut()
    {
        UserId = null;
        SessionId = null;
        DisplayName = null;
        IsAuthenticated = false;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
