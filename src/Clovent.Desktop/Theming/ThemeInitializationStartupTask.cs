using Clovent.Platform.Bootstrap;
using DevExpress.Skins;
using Microsoft.Extensions.Options;

namespace Clovent.Desktop.Theming;

/// <summary>
/// Enables DevExpress form skinning and applies <see cref="DesktopOptions.DefaultSkin"/>
/// once, at startup - runs as an <see cref="IStartupTask"/> so it executes
/// through <c>ApplicationBootstrapper.BuildAndInitializeAsync</c>'s existing
/// pipeline rather than needing its own bespoke startup step.
/// </summary>
public sealed class ThemeInitializationStartupTask(IThemeService themeService, IOptions<DesktopOptions> options) : IStartupTask
{
    /// <inheritdoc/>
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        SkinManager.EnableFormSkins();
        SkinManager.EnableMdiFormSkins();

        themeService.ApplySkin(options.Value.DefaultSkin);

        return Task.CompletedTask;
    }
}
