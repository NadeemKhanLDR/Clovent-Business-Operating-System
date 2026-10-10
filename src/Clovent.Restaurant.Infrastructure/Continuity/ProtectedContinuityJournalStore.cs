using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clovent.Restaurant.Continuity;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Infrastructure.Continuity;

/// <summary>
/// Thread-safe, DPAPI-protected file-backed journal store for offline emergency transactions.
/// Stores tamper-evident records encrypted with DPAPI (LocalMachine scope with CurrentUser fallback).
/// </summary>
public sealed class ProtectedContinuityJournalStore : IContinuityJournalStore
{
    private static readonly byte[] Entropy = "Clovent-ContinuityJournal-2026-Secret"u8.ToArray();
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly string _journalFilePath;
    private readonly DataProtectionScope _protectionScope;
    private readonly ILogger<ProtectedContinuityJournalStore>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Initializes a new instance of <see cref="ProtectedContinuityJournalStore"/>.
    /// </summary>
    public ProtectedContinuityJournalStore(
        string? customJournalPath = null,
        DataProtectionScope protectionScope = DataProtectionScope.LocalMachine,
        ILogger<ProtectedContinuityJournalStore>? logger = null)
    {
        _logger = logger;
        _protectionScope = protectionScope;
        if (!string.IsNullOrWhiteSpace(customJournalPath))
        {
            _journalFilePath = customJournalPath;
        }
        else
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrWhiteSpace(programData))
            {
                programData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            var dir = Path.Combine(programData, "Clovent", "BusinessOperatingSystem", "ContinuityJournal");
            _journalFilePath = Path.Combine(dir, "journal.dat");
        }

        var dirPath = Path.GetDirectoryName(_journalFilePath);
        if (!string.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(dirPath))
        {
            try
            {
                Directory.CreateDirectory(dirPath);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var fallbackDir = Path.Combine(localData, "Clovent", "Clovent.BusinessOperatingSystem", "ContinuityJournal");
                _journalFilePath = Path.Combine(fallbackDir, "journal.dat");
                Directory.CreateDirectory(fallbackDir);
                _logger?.LogWarning(ex, "Failed to create directory {DirPath}. Falling back to LocalAppData: {FallbackPath}", dirPath, _journalFilePath);
            }
        }
    }

    /// <summary>Gets the configured cryptographic protection scope (defaults to LocalMachine).</summary>
    public DataProtectionScope ProtectionScope => _protectionScope;

    /// <inheritdoc />
    public async Task AppendAsync(EmergencyTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (!transaction.VerifyChecksum())
        {
            throw new ContinuityTamperException(
                $"Emergency transaction {transaction.TransactionId} checksum validation failed. Tampering or corruption detected.");
        }

        if (string.IsNullOrWhiteSpace(transaction.HmacSignature) || !transaction.VerifyHmacSignature())
        {
            throw new ContinuityTamperException(
                $"Emergency transaction {transaction.TransactionId} cryptographic HMAC signature validation failed. Unauthorized modification detected.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var list = await ReadAllInternalAsync().ConfigureAwait(false);
            var existingIndex = list.FindIndex(x => x.TransactionId == transaction.TransactionId);
            if (existingIndex >= 0)
            {
                list[existingIndex] = transaction;
            }
            else
            {
                // Enforce hash chaining and sequence ordering
                if (list.Count > 0)
                {
                    var prev = list[^1];
                    var expectedPrevHash = prev.HmacSignature;
                    if (!string.Equals(transaction.PreviousTransactionHash, expectedPrevHash, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ContinuityTamperException(
                            $"Broken hash chain for emergency transaction {transaction.TransactionId}. Expected predecessor hash {expectedPrevHash}, got {transaction.PreviousTransactionHash}.");
                    }

                    if (transaction.SequenceNumber != prev.SequenceNumber + 1)
                    {
                        throw new ContinuityTamperException(
                            $"Sequence gap or out-of-order transaction detected for emergency transaction {transaction.TransactionId}. Expected sequence {prev.SequenceNumber + 1}, got {transaction.SequenceNumber}.");
                    }
                }
                else
                {
                    if (!string.Equals(transaction.PreviousTransactionHash, EmergencyTransaction.GenesisHash, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ContinuityTamperException(
                            $"Initial emergency transaction {transaction.TransactionId} must reference GenesisHash {EmergencyTransaction.GenesisHash}.");
                    }
                }

                list.Add(transaction);
            }

            await WriteAllInternalAsync(list).ConfigureAwait(false);
            _logger?.LogInformation(
                "Appended emergency transaction {TransactionId} (Seq: {Seq}, Total: {Total}) to journal",
                transaction.TransactionId, transaction.SequenceNumber, transaction.OrderSnapshot.GrandTotal);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmergencyTransaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var list = await ReadAllInternalAsync().ConfigureAwait(false);
            return list.AsReadOnly();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmergencyTransaction>> GetPendingReplayAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var list = await ReadAllInternalAsync().ConfigureAwait(false);
            return list.Where(x => x.ReconciliationStatus == ReconciliationStatus.PendingReplay)
                       .OrderBy(x => x.SequenceNumber)
                       .ToList()
                       .AsReadOnly();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(EmergencyTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var list = await ReadAllInternalAsync().ConfigureAwait(false);
            var index = list.FindIndex(x => x.TransactionId == transaction.TransactionId);
            if (index < 0)
            {
                throw new KeyNotFoundException($"Transaction {transaction.TransactionId} not found in journal.");
            }

            list[index] = transaction;
            await WriteAllInternalAsync(list).ConfigureAwait(false);
            _logger?.LogInformation(
                "Updated emergency transaction {TransactionId} status to {Status}",
                transaction.TransactionId, transaction.ReconciliationStatus);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ContinuityJournalStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var list = await ReadAllInternalAsync().ConfigureAwait(false);
            var pending = list.Where(x => x.ReconciliationStatus == ReconciliationStatus.PendingReplay).ToList();
            var replayed = list.Count(x => x.ReconciliationStatus == ReconciliationStatus.Replayed);
            var failedOrConflict = list.Count(x => x.ReconciliationStatus is ReconciliationStatus.Conflict
                                                                         or ReconciliationStatus.Failed
                                                                         or ReconciliationStatus.RequiresManagerReview);
            var oldestPending = pending.Count > 0 ? pending.Min(x => x.TimestampUtc) : (DateTimeOffset?)null;

            return new ContinuityJournalStatistics(
                TotalRecorded: list.Count,
                PendingReplayCount: pending.Count,
                ReplayedCount: replayed,
                FailedOrConflictCount: failedOrConflict,
                OldestPendingAtUtc: oldestPending);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var list = await ReadAllInternalAsync().ConfigureAwait(false);
            var maxSeq = list.Count > 0 ? list.Max(x => x.SequenceNumber) : 0;
            return maxSeq + 1;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> GetLastTransactionHashAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var list = await ReadAllInternalAsync().ConfigureAwait(false);
            var last = list.LastOrDefault();
            if (last == null) return EmergencyTransaction.GenesisHash;
            return !string.IsNullOrEmpty(last.HmacSignature) ? last.HmacSignature : (!string.IsNullOrEmpty(last.Checksum) ? last.Checksum : EmergencyTransaction.GenesisHash);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<EmergencyTransaction>> ReadAllInternalAsync()
    {
        if (!File.Exists(_journalFilePath))
        {
            return new List<EmergencyTransaction>();
        }

        var lines = await File.ReadAllLinesAsync(_journalFilePath).ConfigureAwait(false);
        var result = new List<EmergencyTransaction>(lines.Length);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var decrypted = DecryptLine(line.Trim());
                if (string.IsNullOrWhiteSpace(decrypted)) continue;

                var tx = JsonSerializer.Deserialize<EmergencyTransaction>(decrypted, JsonOpts);
                if (tx != null)
                {
                    result.Add(tx);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to decrypt or deserialize emergency journal record using scope {Scope}", _protectionScope);
                throw;
            }
        }

        return result;
    }

    private async Task WriteAllInternalAsync(List<EmergencyTransaction> transactions)
    {
        var tempFile = _journalFilePath + ".tmp";
        var lines = new List<string>(transactions.Count);

        foreach (var tx in transactions)
        {
            var json = JsonSerializer.Serialize(tx, JsonOpts);
            var encrypted = EncryptLine(json);
            lines.Add(encrypted);
        }

        await File.WriteAllLinesAsync(tempFile, lines, Encoding.UTF8).ConfigureAwait(false);
        File.Move(tempFile, _journalFilePath, overwrite: true);
    }

    private string EncryptLine(string plainText)
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
            if (_protectionScope == DataProtectionScope.LocalMachine)
            {
                throw new ContinuitySecurityException(
                    $"Machine-level cryptographic protection (DPAPI LocalMachine) failed: {ex.Message}. CBOS Continuity Mode requires machine-level protection so offline journals remain recoverable by any authorized POS user on this workstation. Ensure the application has access to local machine data protection or explicitly configure user-level scope.",
                    ex);
            }

            throw new ContinuitySecurityException(
                $"Cryptographic protection failed using scope {_protectionScope}: {ex.Message}",
                ex);
        }
    }

    private string DecryptLine(string cipherBase64)
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
            throw new ContinuitySecurityException(
                $"Cryptographic decryption of emergency journal failed using scope {_protectionScope}: {ex.Message}. Verify that the journal was created on this machine and that permissions have not changed.",
                ex);
        }
    }
}
