using Clovent.Desktop.Sessions;
using Clovent.Platform.Execution;

namespace Clovent.Desktop.Security;

/// <summary>
/// Default desktop implementation of <see cref="IInteractiveScopeProvider"/>.
/// Maintains the active, authoritatively verified execution context for the desktop session.
/// Bound at the MediatR request-dispatch boundary to supply ambient scope.
/// Automatically clears and invalidates scope when the desktop session signs out.
/// </summary>
public sealed class DesktopInteractiveScopeProvider : IInteractiveScopeProvider
{
    private static readonly AsyncLocal<bool> Suppressed = new();
    private readonly ICurrentSession? _currentSession;
    private volatile IExecutionContext? _currentContext;

    /// <summary>Initializes a new instance of <see cref="DesktopInteractiveScopeProvider"/>.</summary>
    /// <param name="currentSession">Optional current desktop session to track authentication changes.</param>
    public DesktopInteractiveScopeProvider(ICurrentSession? currentSession = null)
    {
        _currentSession = currentSession;
        if (_currentSession != null)
        {
            _currentSession.Changed += (sender, args) =>
            {
                if (!_currentSession.IsAuthenticated)
                {
                    Clear();
                }
            };
        }
    }

    /// <inheritdoc />
    public IExecutionContext? GetCurrentContext()
    {
        if (Suppressed.Value)
        {
            return null;
        }

        if (_currentSession != null && !_currentSession.IsAuthenticated)
        {
            _currentContext = null;
            return null;
        }

        return _currentContext;
    }

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

    /// <inheritdoc />
    public IDisposable Suppress()
    {
        if (Suppressed.Value)
        {
            return SuppressionScope.AlreadySuppressed;
        }

        Suppressed.Value = true;
        return new SuppressionScope();
    }

    private sealed class SuppressionScope : IDisposable
    {
        public static readonly SuppressionScope AlreadySuppressed = new(shouldReset: false);
        private readonly bool _shouldReset;
        private bool _disposed;

        public SuppressionScope(bool shouldReset = true)
        {
            _shouldReset = shouldReset;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_shouldReset)
                {
                    Suppressed.Value = false;
                }
            }
        }
    }
}
