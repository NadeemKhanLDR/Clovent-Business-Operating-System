using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clovent.Restaurant.Continuity;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Infrastructure.Continuity;

/// <summary>
/// Thread-safe, DPAPI-protected, file-backed local operational cache store.
/// Stores terminal-scoped operational snapshots encrypted with machine-level DPAPI
/// and verified with keyed HMAC-SHA256 signatures and atomic file replacements.
/// </summary>
public sealed class ProtectedOperationalCacheStore : IOperationalCacheStore
{
    private static readonly byte[] Entropy = "Clovent-OperationalCache-2026-Secret"u8.ToArray();
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly string _cacheDirectory;
    private readonly DataProtectionScope _protectionScope;
    private readonly ILogger<ProtectedOperationalCacheStore>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private sealed class SerializedEnvelope
    {
        public OperationalCacheMetadata Metadata { get; set; } = null!;
        public OperationalCachePayload Payload { get; set; } = null!;
    }

    /// <summary>Initializes a new instance of <see cref="ProtectedOperationalCacheStore"/>.</summary>
    public ProtectedOperationalCacheStore(
        string? customDirectory = null,
        DataProtectionScope protectionScope = DataProtectionScope.LocalMachine,
        ILogger<ProtectedOperationalCacheStore>? logger = null)
    {
        _logger = logger;
        _protectionScope = protectionScope;

        if (!string.IsNullOrWhiteSpace(customDirectory))
        {
            _cacheDirectory = customDirectory;
        }
        else
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrWhiteSpace(programData))
            {
                programData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            _cacheDirectory = Path.Combine(programData, "Clovent", "BusinessOperatingSystem", "OperationalCache");
        }

        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    /// <summary>Gets the directory where terminal cache files are stored.</summary>
    public string CacheDirectory => _cacheDirectory;

    /// <inheritdoc />
    public async Task SaveSnapshotAsync(OperationalCacheSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!snapshot.VerifyIntegrity())
        {
            throw new ContinuityTamperException(
                $"Cannot persist operational cache for terminal {snapshot.Metadata.TerminalId}: HMAC signature or checksum verification failed.");
        }

        var terminalId = snapshot.Metadata.TerminalId;
        var filePath = GetCacheFilePath(terminalId);
        var tempPath = filePath + ".tmp";

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var envelope = new SerializedEnvelope
            {
                Metadata = snapshot.Metadata,
                Payload = snapshot.Payload
            };

            var json = JsonSerializer.Serialize(envelope, JsonOpts);
            var encrypted = Encrypt(json);

            await File.WriteAllTextAsync(tempPath, encrypted, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

            // Verify readability of written temp file before atomic swap
            var testRead = await File.ReadAllTextAsync(tempPath, cancellationToken).ConfigureAwait(false);
            var testDecrypted = Decrypt(testRead);
            if (string.IsNullOrWhiteSpace(testDecrypted))
            {
                throw new InvalidOperationException("Atomic cache verification failed: decrypted content is empty.");
            }

            File.Move(tempPath, filePath, overwrite: true);

            _logger?.LogInformation(
                "Successfully saved operational cache for terminal {TerminalId} ({TerminalCode}) [Version: {Version}, Variants: {Count}]",
                terminalId, snapshot.Metadata.TerminalCode, snapshot.Metadata.CacheVersion, snapshot.Payload.Variants.Count);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<OperationalCacheSnapshot?> LoadSnapshotAsync(Guid terminalId, CancellationToken cancellationToken = default)
    {
        var filePath = GetCacheFilePath(terminalId);
        if (!File.Exists(filePath))
        {
            _logger?.LogDebug("No operational cache file found for terminal {TerminalId} at {Path}", terminalId, filePath);
            return null;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var encrypted = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(encrypted))
            {
                _logger?.LogWarning("Operational cache file for terminal {TerminalId} is empty.", terminalId);
                return null;
            }

            var decrypted = Decrypt(encrypted);
            var envelope = JsonSerializer.Deserialize<SerializedEnvelope>(decrypted, JsonOpts);
            if (envelope?.Metadata == null || envelope?.Payload == null)
            {
                _logger?.LogError("Failed to deserialize operational cache envelope for terminal {TerminalId}", terminalId);
                return null;
            }

            var snapshot = new OperationalCacheSnapshot(envelope.Metadata, envelope.Payload);
            if (!snapshot.VerifyIntegrity())
            {
                _logger?.LogError("Operational cache file for terminal {TerminalId} failed HMAC integrity verification. Possible tampering.", terminalId);
                throw new ContinuityTamperException($"Operational cache for terminal {terminalId} failed cryptographic integrity authentication.");
            }

            return snapshot;
        }
        catch (ContinuityTamperException)
        {
            throw;
        }
        catch (ContinuitySecurityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load operational cache for terminal {TerminalId}", terminalId);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<OperationalCacheMetadata?> GetMetadataAsync(Guid terminalId, CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadSnapshotAsync(terminalId, cancellationToken).ConfigureAwait(false);
        return snapshot?.Metadata;
    }

    /// <inheritdoc />
    public async Task<bool> HasValidCacheAsync(Guid branchId, Guid terminalId, CacheFreshnessPolicy policy, CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadSnapshotAsync(terminalId, cancellationToken).ConfigureAwait(false);
        if (snapshot == null) return false;

        var (status, _) = snapshot.Validate(branchId, terminalId, policy);
        return status is CacheValidationStatus.Valid or CacheValidationStatus.StaleWithinPolicy;
    }

    private string GetCacheFilePath(Guid terminalId) =>
        Path.Combine(_cacheDirectory, $"cache_terminal_{terminalId:N}.dat");

    private string Encrypt(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        try
        {
            var protectedBytes = ProtectedData.Protect(bytes, Entropy, _protectionScope);
            return Convert.ToBase64String(protectedBytes);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DPAPI encryption failed using scope {Scope}", _protectionScope);
            throw new ContinuitySecurityException($"Operational cache encryption failed: {ex.Message}", ex);
        }
    }

    private string Decrypt(string cipherBase64)
    {
        try
        {
            var cipherBytes = Convert.FromBase64String(cipherBase64);
            var plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, _protectionScope);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DPAPI decryption failed using scope {Scope}", _protectionScope);
            throw new ContinuitySecurityException($"Operational cache decryption failed: {ex.Message}", ex);
        }
    }
}
