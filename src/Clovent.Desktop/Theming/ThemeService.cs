using DevExpress.LookAndFeel;
using DevExpress.Skins;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Theming;

/// <summary>
/// <see cref="IThemeService"/> implementation over DevExpress's
/// <see cref="UserLookAndFeel"/>/<see cref="SkinManager"/>. A singleton -
/// <see cref="UserLookAndFeel.Default"/> is itself process-wide, so a second
/// instance would just be a second view over the same ambient state.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;

    /// <summary>Constructs the service and snapshots every DevExpress skin name currently registered.</summary>
    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
        AvailableSkins = SkinManager.Default.Skins
            .Cast<SkinContainer>()
            .Select(skin => skin.SkinName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc/>
    public string CurrentSkin { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public IReadOnlyList<string> AvailableSkins { get; }

    /// <inheritdoc/>
    public event EventHandler? SkinChanged;

    /// <inheritdoc/>
    public void ApplySkin(string skinName)
    {
        if (!AvailableSkins.Contains(skinName, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"'{skinName}' is not a registered DevExpress skin.", nameof(skinName));
        }

        UserLookAndFeel.Default.SetSkinStyle(skinName);
        CurrentSkin = skinName;
        _logger.LogInformation("Applied DevExpress skin '{SkinName}'.", skinName);
        SkinChanged?.Invoke(this, EventArgs.Empty);
    }
}
