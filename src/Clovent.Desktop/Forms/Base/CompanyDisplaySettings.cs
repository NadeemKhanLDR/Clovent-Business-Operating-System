using System;
using System.IO;
using System.Text.Json;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Company display settings model for date format, time format, and quantity decimal precision.
/// </summary>
public sealed class CompanyDisplaySettings
{
    /// <summary>Date display format pattern (e.g. "dd-MMM-yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd").</summary>
    public string DateFormat { get; set; } = "dd-MMM-yyyy";

    /// <summary>Time display mode: "12 Hour" or "24 Hour".</summary>
    public string TimeFormat { get; set; } = "12 Hour";

    /// <summary>Quantity decimal display precision (default 2 decimals).</summary>
    public int QuantityPrecision { get; set; } = 2;
}

/// <summary>
/// Persists and loads company-wide display preferences as a local JSON file.
/// </summary>
public static class CompanyDisplaySettingsStore
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "company_display_settings.json");

    private static readonly object _lock = new();
    private static CompanyDisplaySettings? _cached;

    /// <summary>Resets the in-memory cache for unit testing.</summary>
    internal static void ResetCacheForTesting()
    {
        lock (_lock)
        {
            _cached = null;
        }
    }

    /// <summary>Loads company display settings from disk or defaults.</summary>
    public static CompanyDisplaySettings Load()
    {
        lock (_lock)
        {
            if (_cached != null) return _cached;

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    _cached = JsonSerializer.Deserialize<CompanyDisplaySettings>(json);
                    if (_cached != null) return _cached;
                }
            }
            catch
            {
                // Fallback to fresh defaults
            }

            _cached = new CompanyDisplaySettings();
            return _cached;
        }
    }

    /// <summary>Persists company display settings to disk and updates in-memory cache.</summary>
    public static void Save(CompanyDisplaySettings settings)
    {
        lock (_lock)
        {
            _cached = settings;
            try
            {
                var dir = Path.GetDirectoryName(SettingsFilePath)!;
                Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Ignore filesystem write failure in unprivileged sandbox; cache retains settings
            }
        }
    }
}
