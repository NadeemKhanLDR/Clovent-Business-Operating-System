using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Services;

/// <summary>
/// File-based local active cart checkpoint store. Persists cart state
/// so in-progress orders can be recovered immediately after sudden power loss or process termination.
/// </summary>
public sealed class ActiveOrderCheckpointStore : IActiveOrderCheckpointStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _checkpointDirectory;
    private readonly ILogger<ActiveOrderCheckpointStore>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of <see cref="ActiveOrderCheckpointStore"/>.</summary>
    public ActiveOrderCheckpointStore(
        string? customDirectory = null,
        ILogger<ActiveOrderCheckpointStore>? logger = null)
    {
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(customDirectory))
        {
            _checkpointDirectory = customDirectory;
        }
        else
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrWhiteSpace(programData))
            {
                programData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            _checkpointDirectory = Path.Combine(programData, "Clovent", "BusinessOperatingSystem", "CartCheckpoints");
        }

        if (!Directory.Exists(_checkpointDirectory))
        {
            Directory.CreateDirectory(_checkpointDirectory);
        }
    }

    /// <inheritdoc />
    public async Task SaveCheckpointAsync(CartCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        var safeTerminal = SanitizeFileName(checkpoint.TerminalId);
        var path = Path.Combine(_checkpointDirectory, $"terminal_{safeTerminal}.json");
        var tempPath = path + ".tmp";

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(checkpoint, JsonOpts);
            await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, path, overwrite: true);

            _logger?.LogDebug("Saved cart checkpoint for terminal {TerminalId} ({ItemCount} items)",
                checkpoint.TerminalId, checkpoint.Lines.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save cart checkpoint for terminal {TerminalId}", checkpoint.TerminalId);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<CartCheckpoint?> LoadCheckpointAsync(string terminalId, CancellationToken cancellationToken = default)
    {
        var safeTerminal = SanitizeFileName(terminalId);
        var path = Path.Combine(_checkpointDirectory, $"terminal_{safeTerminal}.json");

        if (!File.Exists(path))
        {
            return null;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) return null;

            return JsonSerializer.Deserialize<CartCheckpoint>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load active cart checkpoint for terminal {TerminalId}", terminalId);
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best effort cleanup of corrupt checkpoint
            }
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearCheckpointAsync(string terminalId, CancellationToken cancellationToken = default)
    {
        var safeTerminal = SanitizeFileName(terminalId);
        var path = Path.Combine(_checkpointDirectory, $"terminal_{safeTerminal}.json");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger?.LogDebug("Cleared cart checkpoint for terminal {TerminalId}", terminalId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to delete cart checkpoint for terminal {TerminalId}", terminalId);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "default";
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
