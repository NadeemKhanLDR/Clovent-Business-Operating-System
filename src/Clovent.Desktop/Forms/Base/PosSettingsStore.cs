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

    private class PosSettingsData
    {
        public int ItemsPerRow { get; set; } = 4;
        public string ViewMode { get; set; } = "Grid";
        public bool ActiveOrdersCollapsed { get; set; } = false;
        public string DefaultPaymentMethod { get; set; } = "Cash";
    }

    private static PosSettingsData LoadData()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new PosSettingsData();
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<PosSettingsData>(json) ?? new PosSettingsData();
        }
        catch
        {
            return new PosSettingsData();
        }
    }

    private static void SaveData(PosSettingsData data)
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Ignore write failures in unprivileged environments
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
}
