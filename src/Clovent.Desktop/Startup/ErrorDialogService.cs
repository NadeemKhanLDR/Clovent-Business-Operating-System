namespace Clovent.Desktop.Startup;

/// <summary><see cref="IErrorDialogService"/> implementation showing <see cref="ErrorDialogForm"/> modally.</summary>
/// <remarks>
/// Two of the three surfaces <see cref="GlobalExceptionHandler"/> subscribes to
/// do not report on the UI thread: <see cref="AppDomain.UnhandledException"/>
/// fires on whichever thread faulted, and
/// <see cref="TaskScheduler.UnobservedTaskException"/> fires on the finalizer
/// thread. A modal dialog opened from either has no message pump of its own and
/// no relationship to the application's windows, so it renders behind whatever
/// is on screen and cannot be clicked or dismissed - the operator sees a frozen
/// screen with no visible cause (defect D24). Everything below therefore
/// marshals onto the UI thread and shows the dialog owned by the active window.
/// </remarks>
public sealed class ErrorDialogService : IErrorDialogService
{
    /// <inheritdoc/>
    public void ShowError(Exception exception, string? context = null)
    {
        var owner = ResolveOwner();

        if (owner is not null && owner.InvokeRequired)
        {
            owner.BeginInvoke(() => Show(owner, exception, context));
            return;
        }

        Show(owner, exception, context);
    }

    private static void Show(Form? owner, Exception exception, string? context)
    {
        using var form = new ErrorDialogForm(exception, context);

        if (owner is null || owner.IsDisposed || !owner.IsHandleCreated)
        {
            // No window to own it: TopMost is the only way left to guarantee
            // the operator actually sees this rather than losing it behind a
            // maximized screen.
            form.StartPosition = FormStartPosition.CenterScreen;
            form.TopMost = true;
            form.ShowDialog();
            return;
        }

        form.ShowDialog(owner);
    }

    /// <summary>
    /// The window the dialog should belong to - the active one if the
    /// application has one, otherwise any open window, so the dialog is
    /// always parented to something the operator is actually looking at.
    /// </summary>
    private static Form? ResolveOwner()
    {
        var active = Form.ActiveForm;
        if (active is { IsDisposed: false })
        {
            return active;
        }

        foreach (Form open in Application.OpenForms)
        {
            if (!open.IsDisposed && open.IsHandleCreated)
            {
                return open;
            }
        }

        return null;
    }
}
