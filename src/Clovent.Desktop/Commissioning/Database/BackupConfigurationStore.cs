using System.Text.Json;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Platform.Configuration;

namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Persisted backup destination and schedule settings for CBOS.
/// </summary>
public sealed class BackupConfiguration
{
    public string? BackupDirectory { get; set; }
    public bool AutoBackupBeforeUpgrade { get; set; } = true;
    public bool ScheduledBackupEnabled { get; set; } = false;
    public int ScheduledBackupIntervalHours { get; set; } = 24;
    public int RetentionCount { get; set; } = 14;
    public DateTimeOffset? LastScheduledBackupUtc { get; set; }
}

/// <summary>
/// Reads and writes <see cref="BackupConfiguration"/> atomically.
/// </summary>
public sealed class BackupConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Lock FileLock = new();

    private readonly string _filePath;

    public BackupConfigurationStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? GetDefaultFilePath()
            : filePath;
    }

    public static string GetDefaultFilePath()
    {
        return Path.Combine(ProgramDataAclManager.ConfigDirectory, "backup_settings.json");
    }

    public BackupConfiguration Load()
    {
        lock (FileLock)
        {
            var path = File.Exists(_filePath) ? _filePath : (File.Exists(GetFallbackPath()) ? GetFallbackPath() : null);
            if (path is null)
            {
                return new BackupConfiguration();
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<BackupConfiguration>(json, JsonOptions) ?? new BackupConfiguration();
            }
            catch
            {
                return new BackupConfiguration();
            }
        }
    }

    public void Save(BackupConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        lock (FileLock)
        {
            if (configuration.ScheduledBackupIntervalHours < 1)
            {
                configuration.ScheduledBackupIntervalHours = 1;
            }

            if (configuration.RetentionCount < 0)
            {
                configuration.RetentionCount = 0;
            }

            var json = JsonSerializer.Serialize(configuration, JsonOptions);
            try
            {
                AtomicFileWriter.WriteStringAtomic(_filePath, json);
            }
            catch (UnauthorizedAccessException)
            {
                AtomicFileWriter.WriteStringAtomic(GetFallbackPath(), json);
            }
            catch (IOException)
            {
                AtomicFileWriter.WriteStringAtomic(GetFallbackPath(), json);
            }
        }
    }

    private static string GetFallbackPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Clovent", "Clovent.BusinessOperatingSystem", "backup_settings.json");
    }
}
