using System.Text.Json;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Platform.Configuration;

namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Represents a recorded backup, verification, or restore operation.
/// </summary>
public sealed record BackupHistoryEntry(
    Guid Id,
    DateTimeOffset TimestampUtc,
    string Operation,
    string DatabaseName,
    string BackupPath,
    long? SizeBytes,
    bool Verified,
    bool Success,
    string Message,
    string PerformedBy);

/// <summary>
/// Persists recent database backup, verification, and restore history entries atomically
/// without storing credentials or connection strings.
/// </summary>
public sealed class BackupHistoryStore
{
    private const int MaxEntries = 200;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Lock FileLock = new();

    private readonly string _filePath;

    public BackupHistoryStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? GetDefaultFilePath()
            : filePath;
    }

    public static string GetDefaultFilePath()
    {
        return Path.Combine(ProgramDataAclManager.ConfigDirectory, "backup_history.json");
    }

    public IReadOnlyList<BackupHistoryEntry> LoadAll()
    {
        lock (FileLock)
        {
            var path = ResolveReadablePath();
            if (path is null)
            {
                return [];
            }

            try
            {
                var json = File.ReadAllText(path);
                var entries = JsonSerializer.Deserialize<List<BackupHistoryEntry>>(json, JsonOptions);
                return entries?
                    .OrderByDescending(e => e.TimestampUtc)
                    .ToList() ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    public BackupHistoryEntry? GetLastSuccessfulBackup()
    {
        return LoadAll().FirstOrDefault(e =>
            string.Equals(e.Operation, "Backup", StringComparison.OrdinalIgnoreCase) &&
            e.Success);
    }

    public void Record(BackupHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (FileLock)
        {
            var entries = LoadAll().ToList();
            entries.Insert(0, entry);
            if (entries.Count > MaxEntries)
            {
                entries = entries.Take(MaxEntries).ToList();
            }

            var json = JsonSerializer.Serialize(entries, JsonOptions);
            TryWriteAtomic(_filePath, json);
        }
    }

    private string? ResolveReadablePath()
    {
        if (File.Exists(_filePath))
        {
            return _filePath;
        }

        var fallback = GetFallbackPath();
        return File.Exists(fallback) ? fallback : null;
    }

    private static string GetFallbackPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Clovent", "Clovent.BusinessOperatingSystem", "backup_history.json");
    }

    private static void TryWriteAtomic(string primaryPath, string json)
    {
        try
        {
            AtomicFileWriter.WriteStringAtomic(primaryPath, json);
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
