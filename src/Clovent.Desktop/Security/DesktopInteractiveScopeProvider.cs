using Clovent.Platform.Execution;

namespace Clovent.Desktop.Security;

/// <summary>
/// Default desktop implementation of <see cref="IInteractiveScopeProvider"/>.
/// Maintains the active, authoritatively verified execution context for the desktop session.
/// Bound at the MediatR request-dispatch boundary to supply ambient scope.
/// </summary>
public sealed class DesktopInteractiveScopeProvider : IInteractiveScopeProvider
{
    private volatile IExecutionContext? _currentContext;

    /// <inheritdoc />
    public IExecutionContext? GetCurrentContext() => _currentContext;

    /// <inheritdoc />
    public void SetCurrentContext(IExecutionContext context)
    {
        _currentContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public void Clear()
    {
        _currentContext = null;
    }
}
