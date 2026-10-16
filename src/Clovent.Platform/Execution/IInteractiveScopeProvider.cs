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
}
