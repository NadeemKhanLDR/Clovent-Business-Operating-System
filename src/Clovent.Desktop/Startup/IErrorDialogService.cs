namespace Clovent.Desktop.Startup;

/// <summary>Shows the global "something went wrong" dialog for an otherwise-unhandled exception.</summary>
public interface IErrorDialogService
{
    /// <summary>Displays <paramref name="exception"/> to the user.</summary>
    /// <param name="exception">The exception to display.</param>
    /// <param name="context">Optional short description of where the exception was caught (e.g. <c>"UI thread"</c>), shown alongside the message.</param>
    void ShowError(Exception exception, string? context = null);
}
