namespace Clovent.Desktop.Theming;

/// <summary>
/// Runtime theme (DevExpress skin) switching, used by the theme selector on
/// the Login form (Milestone 8) and the Shell's profile menu (Milestone 11).
/// </summary>
public interface IThemeService
{
    /// <summary>The skin name currently applied.</summary>
    string CurrentSkin { get; }

    /// <summary>Every skin name this service knows how to apply.</summary>
    IReadOnlyList<string> AvailableSkins { get; }

    /// <summary>Applies a skin by name across every open form.</summary>
    /// <exception cref="ArgumentException"><paramref name="skinName"/> is not one of <see cref="AvailableSkins"/>.</exception>
    void ApplySkin(string skinName);

    /// <summary>Raised after <see cref="ApplySkin"/> successfully changes <see cref="CurrentSkin"/>.</summary>
    event EventHandler? SkinChanged;
}
