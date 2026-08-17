namespace Clovent.Desktop.Forms.Base.Localization;

/// <summary>
/// The chosen display language ("en"/"ur"), persisted as a plain text file
/// under the current user's local application data folder - the same
/// file-convention-not-a-database-column choice
/// <c>Forms.Restaurant.MenuItems.MenuItemImageStore</c> already establishes
/// for a Desktop-local preference nothing outside this process needs to
/// know about. Read once at startup (<see cref="LanguageInitializationStartupTask"/>)
/// and again whenever <c>RestaurantSetupView</c> saves a new choice.
/// </summary>
internal static class LanguagePreferenceStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "language.txt");

    /// <summary>The default/fallback culture code when nothing has been saved yet.</summary>
    public const string DefaultCultureCode = "en";

    /// <summary>Reads the persisted culture code, or <see cref="DefaultCultureCode"/> if none has been saved yet.</summary>
    public static string Load()
    {
        try
        {
            return File.Exists(FilePath) ? File.ReadAllText(FilePath).Trim() : DefaultCultureCode;
        }
        catch (IOException)
        {
            return DefaultCultureCode;
        }
    }

    /// <summary>Persists <paramref name="cultureCode"/> (e.g. "en", "ur") as the chosen display language.</summary>
    public static void Save(string cultureCode)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, cultureCode);
    }
}
