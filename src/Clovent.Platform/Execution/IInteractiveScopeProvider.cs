namespace Clovent.Platform.Execution;

/// <summary>
/// Source that provides the trusted execution context for the current interactive desktop session,
/// queried at the request-dispatch boundary to bind ambient execution scope.
/// </summary>
public interface IInteractiveScopeProvider
{
    /// <summary>
    /// Returns the currently active, verified execution context, or <see langword="null"/> if unauthenticated or out of scope.
    /// </summary>
    IExecutionContext? GetCurrentContext();

    /// <summary>
    /// Sets the verified active execution context upon successful authentication or scope transition.
    /// </summary>
    void SetCurrentContext(IExecutionContext context);

    /// <summary>
    /// Clears the active execution context upon logout or session termination.
    /// </summary>
    void Clear();

    /// <summary>
    /// Suppresses interactive scope resolution in the current async execution flow.
    /// Returns a disposable that restores interactive scope resolution when disposed.
    /// </summary>
    IDisposable Suppress() => NullDisposable.Instance;

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}
