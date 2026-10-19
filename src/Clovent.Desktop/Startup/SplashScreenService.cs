using DevExpress.XtraSplashScreen;

namespace Clovent.Desktop.Startup;

/// <summary>
/// <see cref="ISplashScreenService"/> implementation hosting <see cref="StartupWaitForm"/>
/// via DevExpress's <see cref="SplashScreenManager"/> - ensuring high-DPI awareness,
/// proper window sizing, and zero title truncation across all displays.
/// </summary>
public sealed class SplashScreenService : ISplashScreenService
{
    private static readonly object _syncLock = new();
    private static volatile bool _isFormDisplayed;

    /// <inheritdoc/>
    public void Show(string caption, string description)
    {
        lock (_syncLock)
        {
            var isAlreadyShowing = _isFormDisplayed
                || (SplashScreenManager.Default != null && SplashScreenManager.Default.IsSplashFormVisible)
                || SplashScreenManager.FormInPendingState;

            if (!isAlreadyShowing)
            {
                try
                {
                    SplashScreenManager.ShowForm(typeof(StartupWaitForm), useFadeIn: true, useFadeOut: true);
                    _isFormDisplayed = true;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("already been displayed", StringComparison.OrdinalIgnoreCase))
                {
                    // Overlapping display request: splash is already displayed
                    _isFormDisplayed = true;
                }
            }

            SplashScreenManager.Default?.SetWaitFormCaption(caption);
            SplashScreenManager.Default?.SetWaitFormDescription(description);
        }
    }

    /// <inheritdoc/>
    public void SetDescription(string description)
    {
        lock (_syncLock)
        {
            SplashScreenManager.Default?.SetWaitFormDescription(description);
        }
    }

    /// <inheritdoc/>
    public void Close()
    {
        lock (_syncLock)
        {
            var isShowing = _isFormDisplayed
                || (SplashScreenManager.Default != null && SplashScreenManager.Default.IsSplashFormVisible);

            if (!isShowing)
            {
                return;
            }

            _isFormDisplayed = false;
            try
            {
                SplashScreenManager.CloseForm(throwExceptionIfAlreadyClosed: false);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not displayed", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("already closed", StringComparison.OrdinalIgnoreCase))
            {
                // Already closed or not displayed
            }
        }
    }
}
