namespace Clovent.Platform.Execution;

/// <summary>
/// Pushes an <see cref="IExecutionContext"/> as the ambient context for the
/// lifetime of the scope, restoring the previous value on dispose. This is
/// the only supported way to change what <see cref="IExecutionContextAccessor.Current"/>
/// reports - construct one via <see cref="ExecutionContextAccessorExtensions.BeginScope"/>
/// rather than directly, in a <see langword="using"/> block around the unit
/// of work the context applies to.
/// </summary>
public sealed class ExecutionContextScope : IDisposable
{
    private readonly ExecutionContextAccessor _accessor;
    private readonly IExecutionContext? _previous;
    private bool _disposed;

    /// <summary>
    /// Captures the accessor's current context as "previous", then makes
    /// <paramref name="context"/> the ambient context until this scope is
    /// disposed. Takes the concrete <see cref="ExecutionContextAccessor"/>
    /// rather than the interface deliberately - mutation is not exposed
    /// through <see cref="IExecutionContextAccessor"/> itself.
    /// </summary>
    public ExecutionContextScope(ExecutionContextAccessor accessor, IExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(context);

        _accessor = accessor;
        _previous = accessor.Current;
        accessor.Current = context;
    }

    /// <summary>Restores whatever execution context was ambient before this scope began. Safe to call more than once.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _accessor.Current = _previous;
    }
}

/// <summary>
/// Entry point for pushing an ambient <see cref="IExecutionContext"/>.
/// </summary>
public static class ExecutionContextAccessorExtensions
{
    /// <summary>
    /// Begins an <see cref="ExecutionContextScope"/> that makes <paramref name="context"/>
    /// the ambient execution context until the returned <see cref="IDisposable"/>
    /// is disposed (typically via a <see langword="using"/> statement).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="accessor"/> is not the built-in <see cref="ExecutionContextAccessor"/>.
    /// Only that implementation supports mutation; a custom
    /// <see cref="IExecutionContextAccessor"/> would need its own scope mechanism.
    /// </exception>
    public static IDisposable BeginScope(this IExecutionContextAccessor accessor, IExecutionContext context)
    {
        if (accessor is not ExecutionContextAccessor concrete)
        {
            throw new InvalidOperationException(
                $"BeginScope requires the default {nameof(ExecutionContextAccessor)} implementation; " +
                $"'{accessor.GetType().Name}' does not support mutating the ambient execution context.");
        }

        return new ExecutionContextScope(concrete, context);
    }
}
