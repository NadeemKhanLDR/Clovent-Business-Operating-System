namespace Clovent.Desktop.Sessions;

/// <summary>
/// The desktop host's own "who is signed in right now" state - long-lived
/// for the app's session, in contrast to <c>Clovent.Platform.Execution.IExecutionContext</c>,
/// which is scoped ambient state for a single unit of work (a command, a
/// query) and is restored/cleared via <c>ExecutionContextScope</c> around
/// each one. Once a per-operation command dispatch pipeline exists, that
/// pipeline is expected to open an <c>ExecutionContextScope</c> derived
/// <em>from</em> this session's <see cref="UserId"/> - the two are
/// complementary, not redundant.
/// </summary>
public interface ICurrentSession
{
    /// <summary>Whether a user is currently signed in.</summary>
    bool IsAuthenticated { get; }

    /// <summary>The signed-in user's id, or <see langword="null"/> if <see cref="IsAuthenticated"/> is <see langword="false"/>.</summary>
    Guid? UserId { get; }

    /// <summary>The active <c>Session</c> aggregate's id, or <see langword="null"/> if <see cref="IsAuthenticated"/> is <see langword="false"/>.</summary>
    Guid? SessionId { get; }

    /// <summary>The signed-in user's display name, or <see langword="null"/> if <see cref="IsAuthenticated"/> is <see langword="false"/> - shown by the Shell's profile menu (Milestone 11).</summary>
    string? DisplayName { get; }

    /// <summary>Establishes the current session after a successful login.</summary>
    void SignIn(Guid userId, Guid sessionId, string displayName);

    /// <summary>Clears the current session.</summary>
    void SignOut();

    /// <summary>Raised whenever <see cref="IsAuthenticated"/> changes, from either <see cref="SignIn"/> or <see cref="SignOut"/>.</summary>
    event EventHandler? Changed;
}
