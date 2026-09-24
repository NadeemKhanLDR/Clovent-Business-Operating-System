using System;
using System.IO;
using System.Text.Json;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Persists user preferences for the Restaurant POS (View Mode and Items Per Row)
/// as a local JSON file. Follows the established CBOS file convention pattern.
/// </summary>
public static class PosSettingsStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "pos_settings.json");

    private static readonly object _lock = new();
    private static PosSettingsData? _cachedData;

    private class PosSettingsData
    {
        public int ItemsPerRow { get; set; } = 4;
        public string ViewMode { get; set; } = "Grid";
        public bool ActiveOrdersCollapsed { get; set; } = false;
        public string DefaultPaymentMethod { get; set; } = "Cash";
        public int OrderHealthGreenMinutes { get; set; } = 10;
        public int OrderHealthOrangeMinutes { get; set; } = 20;
        public bool RushModeEnabled { get; set; } = false;
        public Guid? TerminalId { get; set; }
        public Guid? BranchId { get; set; }
    }

    private static PosSettingsData LoadData()
    {
        lock (_lock)
        {
            if (_cachedData != null)
            {
                return _cachedData;
            }

            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    _cachedData = JsonSerializer.Deserialize<PosSettingsData>(json) ?? new PosSettingsData();
                    return _cachedData;
                }
            }
            catch
            {
                // Fallback to fresh instance
            }

            _cachedData = new PosSettingsData();
            return _cachedData;
        }
    }

    private static void SaveData(PosSettingsData data)
    {
        lock (_lock)
        {
            _cachedData = data;
            try
            {
                var directory = Path.GetDirectoryName(FilePath)!;
                Directory.CreateDirectory(directory);
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Ignore write failures in unprivileged environments; in-memory cache preserves setting
            }
        }
    }

    public static int LoadItemsPerRow()
    {
        var val = LoadData().ItemsPerRow;
        return val >= 4 && val <= 8 ? val : 4;
    }

    public static void SaveItemsPerRow(int itemsPerRow)
    {
        var data = LoadData();
        data.ItemsPerRow = itemsPerRow;
        SaveData(data);
    }

    public static string LoadViewMode()
    {
        var mode = LoadData().ViewMode;
        return string.Equals(mode, "List", StringComparison.OrdinalIgnoreCase) ? "List" : "Grid";
    }

    public static void SaveViewMode(string viewMode)
    {
        var data = LoadData();
        data.ViewMode = viewMode;
        SaveData(data);
    }

    /// <summary>Returns <see langword="true"/> when the user last collapsed the Active Orders sidebar.</summary>
    public static bool LoadActiveOrdersCollapsed()
    {
        return LoadData().ActiveOrdersCollapsed;
    }

    /// <summary>Persists the Active Orders sidebar collapsed/expanded preference.</summary>
    public static void SaveActiveOrdersCollapsed(bool collapsed)
    {
        var data = LoadData();
        data.ActiveOrdersCollapsed = collapsed;
        SaveData(data);
    }

    /// <summary>Returns the configured default payment method name, defaulting to "Cash".</summary>
    public static string LoadDefaultPaymentMethod()
    {
        var method = LoadData().DefaultPaymentMethod;
        return string.IsNullOrWhiteSpace(method) ? "Cash" : method;
    }

    /// <summary>Persists the default payment method preference.</summary>
    public static void SaveDefaultPaymentMethod(string method)
    {
        var data = LoadData();
        data.DefaultPaymentMethod = string.IsNullOrWhiteSpace(method) ? "Cash" : method;
        SaveData(data);
    }

    /// <summary>
    /// Loads the order wait-time health thresholds for the Active Orders rail.
    /// Invalid persisted values (non-positive or unordered) fall back to the
    /// 10/20 defaults rather than ever producing a throwing configuration.
    /// </summary>
    public static (int GreenMinutes, int OrangeMinutes) LoadOrderHealthThresholds()
    {
        var data = LoadData();
        if (data.OrderHealthGreenMinutes > 0
            && data.OrderHealthOrangeMinutes > 0
            && data.OrderHealthGreenMinutes < data.OrderHealthOrangeMinutes)
        {
            return (data.OrderHealthGreenMinutes, data.OrderHealthOrangeMinutes);
        }

        return (10, 20);
    }

    /// <summary>Persists the order wait-time health thresholds.</summary>
    public static void SaveOrderHealthThresholds(int greenMinutes, int orangeMinutes)
    {
        if (greenMinutes <= 0 || orangeMinutes <= 0 || greenMinutes >= orangeMinutes)
        {
            return;
        }

        var data = LoadData();
        data.OrderHealthGreenMinutes = greenMinutes;
        data.OrderHealthOrangeMinutes = orangeMinutes;
        SaveData(data);
    }

    /// <summary>Returns whether Rush Mode was enabled the last time the POS ran.</summary>
    public static bool LoadRushModeEnabled() => LoadData().RushModeEnabled;

    /// <summary>Persists the Rush Mode preference (app-level presentation setting).</summary>
    public static void SaveRushModeEnabled(bool enabled)
    {
        var data = LoadData();
        data.RushModeEnabled = enabled;
        SaveData(data);
    }

    /// <summary>Gets the configured workstation POS terminal ID, or null if unconfigured.</summary>
    public static Guid? LoadTerminalId() => LoadData().TerminalId;

    /// <summary>Persists the configured workstation POS terminal ID.</summary>
    public static void SaveTerminalId(Guid? terminalId)
    {
        var data = LoadData();
        data.TerminalId = terminalId;
        SaveData(data);
    }

    /// <summary>Gets the configured workstation default branch ID, or null if unconfigured.</summary>
    public static Guid? LoadBranchId() => LoadData().BranchId;

    /// <summary>Persists the configured workstation default branch ID.</summary>
    public static void SaveBranchId(Guid? branchId)
    {
        var data = LoadData();
        data.BranchId = branchId;
        SaveData(data);
    }
}
