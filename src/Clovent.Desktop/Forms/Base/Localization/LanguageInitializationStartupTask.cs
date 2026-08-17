using System.Globalization;
using Clovent.Platform.Bootstrap;

namespace Clovent.Desktop.Forms.Base.Localization;

/// <summary>
/// Applies the persisted display language (<see cref="LanguagePreferenceStore"/>)
/// once, at startup - the same <see cref="IStartupTask"/> shape
/// <c>Theming.ThemeInitializationStartupTask</c> already uses for the
/// DevExpress skin, run through the same <c>ApplicationBootstrapper</c>
/// pipeline rather than a bespoke startup step.
/// </summary>
public sealed class LanguageInitializationStartupTask : IStartupTask
{
    /// <inheritdoc/>
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var culture = CultureInfo.GetCultureInfo(LanguagePreferenceStore.Load());
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        return Task.CompletedTask;
    }
}
