using System.Text.Json;

namespace Clovent.Desktop.Forms.Base.Appearance;

/// <summary>
/// Persists the owner-configured <see cref="AppearanceRule"/> list as plain
/// JSON under the current user's local application data folder - the same
/// file-convention-not-a-database-column choice already established this
/// pass for every other Desktop-local, single-machine preference
/// (<c>WindowPlacementStore</c>, <c>LanguagePreferenceStore</c>,
/// <c>MenuItemImageStore</c>). Appearance/font/color configuration is a
/// WinForms presentation concern with no meaning outside this rendering
/// process - it has no business invariant a Domain/Application layer would
/// need to protect, so it deliberately stays out of
/// <c>Clovent.Restaurant</c>/EF Core entirely rather than becoming a new
/// bounded-context aggregate purely to hold <see cref="System.Drawing.Font"/>/
/// <see cref="System.Drawing.Color"/> data - the same reasoning
/// <c>MenuItemImageStore</c>'s own doc comment already applies to photos.
/// The <b>Restaurant Setup -&gt; Appearance</b> screen is a genuine Desktop UI
/// entry point for it, but the data itself belongs with the rendering layer
/// that's the only thing that can ever use it.
/// </summary>
internal static class AppearanceSettingsStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "appearance.json");

    /// <summary>Reads every configured rule, or an empty list if none has been saved yet (or the file is unreadable/corrupt).</summary>
    public static IReadOnlyList<AppearanceRule> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return [];
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<AppearanceRule>>(json) ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return [];
        }
    }

    /// <summary>Persists the full rule list, overwriting whatever was saved before.</summary>
    public static void Save(IReadOnlyList<AppearanceRule> rules)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
