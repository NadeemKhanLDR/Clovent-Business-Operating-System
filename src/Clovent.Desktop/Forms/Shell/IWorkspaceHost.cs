namespace Clovent.Desktop.Forms.Shell;

/// <summary>
/// The Shell's document host - the seam <see cref="Navigation.INavigationService"/>
/// targets when navigating to a view, without depending on <see cref="MainForm"/>
/// directly. Replaces the old single-panel <c>SetContent(Control)</c>: once a
/// document is open it stays open and gets reused (activated) rather than
/// rebuilt, until its tab is closed - see <see cref="MainForm"/> for how it
/// tracks "already open" per key.
/// </summary>
public interface IWorkspaceHost
{
    /// <summary>
    /// Shows the document registered under <paramref name="key"/>: activates
    /// it if already open, otherwise builds it via <paramref name="contentFactory"/>
    /// (called at most once per key unless <paramref name="allowMultipleInstances"/>
    /// is set) and opens a new tab captioned <paramref name="caption"/>.
    /// </summary>
    void ShowDocument(string key, string caption, Func<Control> contentFactory, bool allowMultipleInstances = false);

    /// <summary>Updates the status bar's message text.</summary>
    void SetStatus(string text);
}
