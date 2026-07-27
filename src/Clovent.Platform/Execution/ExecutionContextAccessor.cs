namespace Clovent.Platform.Execution;

/// <summary>
/// AsyncLocal-backed implementation. Uses a wrapping holder object rather
/// than storing the context directly in the AsyncLocal cell - the same
/// pattern ASP.NET Core's HttpContextAccessor uses - so that clearing the
/// value in one async flow does not affect a value already captured by a
/// child flow that branched off earlier.
/// </summary>
public sealed class ExecutionContextAccessor : IExecutionContextAccessor
{
    private static readonly AsyncLocal<ContextHolder> AmbientContext = new();

    public IExecutionContext? Current
    {
        get => AmbientContext.Value?.Context;
        set
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
