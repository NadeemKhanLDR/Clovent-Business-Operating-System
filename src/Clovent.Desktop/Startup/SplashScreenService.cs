using DevExpress.XtraSplashScreen;

namespace Clovent.Desktop.Startup;

/// <summary>
/// <see cref="ISplashScreenService"/> implementation hosting <see cref="StartupWaitForm"/>
/// via DevExpress's <see cref="SplashScreenManager"/> - ensuring high-DPI awareness,
/// proper window sizing, and zero title truncation across all displays.
/// </summary>
public sealed class SplashScreenService : ISplashScreenService
{
    private bool _isOpen;

    /// <inheritdoc/>
    public void Show(string caption, string description)
    {
        if (!_isOpen)
        {
            SplashScreenManager.ShowForm(typeof(StartupWaitForm), useFadeIn: true, useFadeOut: true);
            _isOpen = true;
        }

        SplashScreenManager.Default?.SetWaitFormCaption(caption);
        SplashScreenManager.Default?.SetWaitFormDescription(description);
    }

    /// <inheritdoc/>
    public void SetDescription(string description) =>
        SplashScreenManager.Default?.SetWaitFormDescription(description);

    /// <inheritdoc/>
    public void Close()
    {
        // Closing is idempotent: a close with no splash open is a no-op.
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        try
        {
            SplashScreenManager.CloseForm(throwExceptionIfAlreadyClosed: false);
        }
        catch (InvalidOperationException)
        {
            // Safeguard against any internal DevExpress state discrepancy
        }
    }
}
