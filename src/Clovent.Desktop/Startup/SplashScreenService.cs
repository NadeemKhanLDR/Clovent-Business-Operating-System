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
    /// <inheritdoc/>
    public void Show(string caption, string description) =>
        SplashScreenManager.ShowDefaultWaitForm(caption, description);

    /// <inheritdoc/>
    public void SetDescription(string description) =>
        SplashScreenManager.Default?.SetWaitFormDescription(description);

    /// <inheritdoc/>
    public void Close() =>
        SplashScreenManager.CloseDefaultWaitForm();
}
