namespace Clovent.Platform.Execution;

/// <summary>
/// Pushes an <see cref="IExecutionContext"/> as the ambient context for the
/// lifetime of the scope, restoring the previous value on dispose.
/// </summary>
public sealed class ExecutionContextScope : IDisposable
{
    private readonly IExecutionContextAccessor _accessor;
    private readonly IExecutionContext? _previous;
    private bool _disposed;

    public ExecutionContextScope(IExecutionContextAccessor accessor, IExecutionContext context)
    {
        _accessor = accessor;
        _previous = accessor.Current;
        accessor.Current = context;
    }

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

public static class ExecutionContextAccessorExtensions
{
    public static IDisposable BeginScope(this IExecutionContextAccessor accessor, IExecutionContext context)
        => new ExecutionContextScope(accessor, context);
}
