namespace Clovent.Desktop.Startup;

/// <summary>Shows/hides the splash screen displayed while the host is building and initializing.</summary>
public interface ISplashScreenService
{
    /// <summary>Shows the splash screen with the given caption/description.</summary>
    void Show(string caption, string description);

    /// <summary>Updates the description text on an already-visible splash screen.</summary>
    void SetDescription(string description);

    /// <summary>Closes the splash screen, if visible.</summary>
    void Close();
}
