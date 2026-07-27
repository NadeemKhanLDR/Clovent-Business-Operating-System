namespace Clovent.Platform.Execution;

/// <summary>
/// Read-only ambient access to the current <see cref="IExecutionContext"/> for
/// whatever unit of work (request, UI command, background job) is currently
/// executing. Deliberately shaped like ASP.NET Core's IHttpContextAccessor for
/// familiarity - but implemented purely with System.Threading.AsyncLocal, with
/// no ASP.NET Core dependency, so both a desktop host and a future web host can
/// use it the same way.
/// </summary>
/// <remarks>
/// This interface exposes no setter. Consumers that merely need to know "who/what
/// is executing right now" (loggers, audit trails, repositories applying tenant
/// filters, etc.) can safely depend on this interface without being able to
/// mutate ambient state out from under an in-progress operation. The only
/// supported way to change the current context is <see cref="ExecutionContextScope"/>
/// (via <see cref="ExecutionContextAccessorExtensions.BeginScope"/>), which
/// guarantees the previous context is restored when the scope ends.
/// </remarks>
public interface IExecutionContextAccessor
{
    /// <summary>
    /// The execution context ambient to the currently executing async flow, or
    /// <see langword="null"/> if no <see cref="ExecutionContextScope"/> is active.
    /// </summary>
    IExecutionContext? Current { get; }
}
