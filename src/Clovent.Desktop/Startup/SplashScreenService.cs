using DevExpress.XtraSplashScreen;

namespace Clovent.Desktop.Startup;

/// <summary>
/// <see cref="ISplashScreenService"/> implementation over DevExpress's
/// built-in default wait form (<see cref="SplashScreenManager.ShowDefaultWaitForm(string, string)"/>) -
/// no custom splash form/designer surface required, and no third-party
/// runtime dependency risk beyond DevExpress itself.
/// </summary>
public sealed class SplashScreenService : ISplashScreenService
{
    private bool _isOpen;

    /// <inheritdoc/>
    public void Show(string caption, string description)
    {
        SplashScreenManager.ShowDefaultWaitForm(caption, description);
        _isOpen = true;
    }

    /// <inheritdoc/>
    public void SetDescription(string description) =>
        SplashScreenManager.Default?.SetWaitFormDescription(description);

    /// <inheritdoc/>
    public void Close()
    {
        // CloseDefaultWaitForm throws InvalidOperationException
        // ("Splash Form is not displayed") when no wait form is up - which
        // happens when startup fails before Show(), or Close() runs again
        // from the catch block after a successful close. Closing is
        // idempotent instead: a close with no splash open is a no-op.
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        SplashScreenManager.CloseDefaultWaitForm();
    }
}
