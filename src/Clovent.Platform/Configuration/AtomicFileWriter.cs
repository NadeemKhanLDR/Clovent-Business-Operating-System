using System.Text.Json;

namespace Clovent.Platform.Configuration;

/// <summary>
/// Provides atomic file write operations to guarantee that configuration files,
/// state caches, and persisted journals are never corrupted or left in a 0-byte
/// truncated state if the process or workstation experiences power failure during write.
/// </summary>
public static class AtomicFileWriter
{
    private static readonly object WriteLock = new();

    /// <summary>
    /// Atomically writes serialized JSON data to the target file.
    /// Creates parent directories if missing, writes to a temporary file,
    /// flushes buffers to disk, and replaces the target file atomically.
    /// </summary>
    /// <typeparam name="T">Type of data to serialize.</typeparam>
    /// <param name="targetFilePath">The full path of the destination file.</param>
    /// <param name="content">The object to serialize and persist.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    public static void WriteJsonAtomic<T>(string targetFilePath, T content, JsonSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFilePath);
        ArgumentNullException.ThrowIfNull(content);

        var json = JsonSerializer.Serialize(content, options ?? new JsonSerializerOptions { WriteIndented = true });
        WriteStringAtomic(targetFilePath, json);
    }

    /// <summary>
    /// Atomically writes text to the target file.
    /// Writes to a temporary file (.tmp), flushes to disk, and atomically replaces target.
    /// </summary>
    /// <param name="targetFilePath">The full path of the destination file.</param>
    /// <param name="content">The string content to persist.</param>
    public static void WriteStringAtomic(string targetFilePath, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFilePath);
        ArgumentNullException.ThrowIfNull(content);

        lock (WriteLock)
        {
            var directory = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempFilePath = targetFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                using (var writer = new StreamWriter(fileStream, System.Text.Encoding.UTF8))
                {
                    writer.Write(content);
                    writer.Flush();
                    fileStream.Flush(flushToDisk: true);
                }

                if (File.Exists(targetFilePath))
                {
                    try
                    {
                        File.Replace(tempFilePath, targetFilePath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // Fallback on platforms where File.Replace is not supported
                        File.Delete(targetFilePath);
                        File.Move(tempFilePath, targetFilePath);
                    }
                }
                else
                {
                    File.Move(tempFilePath, targetFilePath);
                }
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // Best-effort cleanup
                    }
                }
            }
        }
    }
}
