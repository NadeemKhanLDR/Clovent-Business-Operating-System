namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Persists the Queue Rush Mode toggle preference locally for this POS workstation.
/// Rush mode streamlines counter operations during peak queue rushes by enabling
/// instant cash buttons and bypassing modal dialogues.
/// </summary>
internal static class QueueRushModeStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "queue-rush-mode.txt");

    private static bool? s_cached;

    /// <summary>Checks whether Queue Rush Mode is currently enabled for this station.</summary>
    public static bool IsEnabled()
    {
        if (s_cached.HasValue) return s_cached.Value;

        try
        {
            if (File.Exists(FilePath))
            {
                var text = File.ReadAllText(FilePath).Trim();
                s_cached = bool.TryParse(text, out var val) && val;
                return s_cached.Value;
            }
        }
        catch (IOException)
        {
            // fallback
        }

        s_cached = false;
        return false;
    }

    /// <summary>Sets and persists whether Queue Rush Mode is enabled.</summary>
    public static void SetEnabled(bool enabled)
    {
        s_cached = enabled;
        try
        {
            var directory = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(FilePath, enabled.ToString());
        }
        catch (IOException)
        {
            // Fallback gracefully
        }
    }
}
