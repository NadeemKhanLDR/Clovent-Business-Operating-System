namespace Clovent.Desktop.Navigation;

/// <summary>
/// Minimal view-registration/navigation framework for the Shell's workspace.
/// A module registers one or more named views (a key plus a factory that
/// creates the <see cref="Control"/> to display); the Shell (or anything
/// else) navigates to a view by key without knowing how it's constructed.
/// </summary>
public interface INavigationService
{
    /// <summary>The key of the view currently displayed, or <see langword="null"/> if none has been navigated to yet.</summary>
    string? CurrentKey { get; }

    /// <summary>Every key currently registered, in registration order.</summary>
    IReadOnlyCollection<string> RegisteredKeys { get; }

    /// <summary>
    /// Registers a view under <paramref name="key"/>. <paramref name="factory"/>
    /// is called fresh on every <see cref="NavigateTo"/> to that key, so a
    /// view's constructor can resolve per-navigation dependencies (e.g. a
    /// freshly-scoped service) rather than being built once and reused.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> is already registered.</exception>
    void Register(string key, Func<Control> factory);

    /// <summary>Removes a previously-registered view. No-ops if <paramref name="key"/> was never registered.</summary>
    void Unregister(string key);

    /// <summary>Creates the view registered under <paramref name="key"/> and displays it in the workspace.</summary>
    /// <exception cref="KeyNotFoundException"><paramref name="key"/> is not registered.</exception>
    void NavigateTo(string key);

    /// <summary>Raised after <see cref="NavigateTo"/> successfully displays a view.</summary>
    event EventHandler<string>? Navigated;
}
