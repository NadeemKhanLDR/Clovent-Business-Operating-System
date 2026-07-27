namespace Clovent.Platform.Execution;

/// <summary>
/// The default, AsyncLocal-backed implementation of <see cref="IExecutionContextAccessor"/>.
/// Uses a wrapping holder object rather than storing the context directly in
/// the AsyncLocal cell - the same pattern ASP.NET Core's HttpContextAccessor
/// uses - so that clearing the value in one async flow does not affect a
/// value already captured by a child flow that branched off earlier.
/// </summary>
/// <remarks>
/// Mutation is intentionally not part of the public <see cref="IExecutionContextAccessor"/>
/// contract. The setter here is <see langword="internal"/> so that, outside this
/// assembly, the ambient context can only be changed through
/// <see cref="ExecutionContextScope"/> - normal consumers holding just the
/// interface cannot replace the current context directly.
/// </remarks>
public sealed class ExecutionContextAccessor : IExecutionContextAccessor
{
    private static readonly AsyncLocal<ContextHolder> AmbientContext = new();

    /// <inheritdoc />
    public IExecutionContext? Current
    {
        get => AmbientContext.Value?.Context;
        internal set
        {
            var holder = AmbientContext.Value;
            if (holder is not null)
            {
                // Clear this flow's holder so a value set later in a
                // sibling/child flow doesn't unexpectedly appear here too.
                holder.Context = null;
            }

            if (value is not null)
            {
                AmbientContext.Value = new ContextHolder { Context = value };
            }
        }
    }

    private sealed class ContextHolder
    {
        public IExecutionContext? Context;
    }
}
