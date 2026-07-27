namespace Clovent.Platform.Execution;

/// <summary>
/// Ambient access to the current <see cref="IExecutionContext"/>. Deliberately
/// shaped like ASP.NET Core's IHttpContextAccessor (a settable Current
/// property) since that shape is familiar - but implemented purely with
/// System.Threading.AsyncLocal, with no ASP.NET Core dependency, so both a
/// desktop host and a future web host can use it the same way.
/// </summary>
public interface IExecutionContextAccessor
{
    IExecutionContext? Current { get; set; }
}
