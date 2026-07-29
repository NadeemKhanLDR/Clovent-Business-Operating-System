using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Startup;

/// <summary>
/// Wires every unhandled-exception surface WinForms exposes to a single
/// log-then-show-dialog handler, so no unhandled exception can silently
/// crash the process (or, worse, silently vanish) without the user seeing
/// something and the log recording it.
/// </summary>
public static class GlobalExceptionHandler
{
    /// <summary>
    /// Registers handlers for <see cref="Application.ThreadException"/> (UI-thread
    /// exceptions), <see cref="AppDomain.UnhandledException"/> (any other thread),
    /// and <see cref="TaskScheduler.UnobservedTaskException"/> (a faulted
    /// <see cref="Task"/> nobody awaited). Call once, before <see cref="Application.Run(Form)"/>.
    /// </summary>
    public static void Initialize(ILogger logger, IErrorDialogService errorDialogService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(errorDialogService);

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (_, e) =>
            Handle(logger, errorDialogService, e.Exception, "UI thread");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Handle(logger, errorDialogService, e.ExceptionObject as Exception ?? new InvalidOperationException(e.ExceptionObject?.ToString() ?? "Unknown error"), "background thread");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Handle(logger, errorDialogService, e.Exception, "unobserved task");
            e.SetObserved();
        };
    }

    private static void Handle(ILogger logger, IErrorDialogService errorDialogService, Exception exception, string context)
    {
        logger.LogError(exception, "Unhandled exception ({Context}).", context);
        errorDialogService.ShowError(exception, context);
    }
}
