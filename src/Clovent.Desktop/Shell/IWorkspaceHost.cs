namespace Clovent.Desktop.Shell;

/// <summary>
/// The Shell's swappable content area - the seam <see cref="Navigation.INavigationService"/>
/// targets when navigating between views, without depending on <see cref="ShellForm"/> directly.
/// </summary>
public interface IWorkspaceHost
{
    /// <summary>Replaces whatever control currently occupies the workspace with <paramref name="content"/>.</summary>
    void SetContent(Control content);
}
