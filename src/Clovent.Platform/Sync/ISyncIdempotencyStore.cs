using System.Collections.Concurrent;

namespace Clovent.Platform.Sync;

/// <summary>Information about a previously processed delta-sync packet.</summary>
public sealed record ProcessedSyncPacketRecord(
    string IdempotencyKey,
    Guid PacketId,
    string EntityKind,
    DateTimeOffset IngestedAtUtc);

/// <summary>
/// Durable or in-memory registry of delta sync packet inbox entries,
/// guaranteeing exact-once processing, identity verification, tamper rejection,
/// and crash recovery across process restarts.
/// </summary>
public interface ISyncIdempotencyStore
{
    /// <summary>Checks whether a packet with this idempotency key has already been successfully ingested and applied.</summary>
    Task<bool> HasBeenProcessedAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the full inbox record for an idempotency key, if one exists.</summary>
    Task<SyncInboxRecord?> GetInboxRecordAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Records initial receipt or transitions of an inbox record.</summary>
    Task RecordInboxAsync(SyncInboxRecord record, CancellationToken cancellationToken = default);

    /// <summary>Updates the processing status of an existing inbox record.</summary>
    Task UpdateStatusAsync(
        string idempotencyKey,
        SyncInboxStatus status,
        DateTimeOffset? processedAtUtc = null,
        string? failureReason = null,
        Guid? stagedConflictId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Records that a packet has been successfully ingested (legacy shortcut for Applied status).</summary>
    Task RecordProcessedAsync(
        string idempotencyKey,
        Guid packetId,
        string entityKind,
        DateTimeOffset ingestedAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves total number of unique packets ingested in Applied status.</summary>
    Task<int> GetProcessedCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Clears recorded history (test-only resets).</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ISyncIdempotencyStore"/>.
/// Marked strictly for test harness usage; production configurations use durable persistence.
/// </summary>
public sealed class InMemorySyncIdempotencyStore : ISyncIdempotencyStore
{
    private readonly ConcurrentDictionary<string, SyncInboxRecord> _records = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<bool> HasBeenProcessedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (_records.TryGetValue(idempotencyKey, out var record))
        {
            return Task.FromResult(record.Status == SyncInboxStatus.Applied);
        }
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<SyncInboxRecord?> GetInboxRecordAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        _records.TryGetValue(idempotencyKey, out var record);
        return Task.FromResult(record);
    }

    /// <inheritdoc/>
    public Task RecordInboxAsync(SyncInboxRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.IdempotencyKey);

        _records[record.IdempotencyKey] = record;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdateStatusAsync(
        string idempotencyKey,
        SyncInboxStatus status,
        DateTimeOffset? processedAtUtc = null,
        string? failureReason = null,
        Guid? stagedConflictId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        if (_records.TryGetValue(idempotencyKey, out var record))
        {
            record.Status = status;
            record.ProcessedAtUtc = processedAtUtc ?? (status == SyncInboxStatus.Applied ? DateTimeOffset.UtcNow : record.ProcessedAtUtc);
            if (failureReason != null) record.FailureReason = failureReason;
            if (stagedConflictId != null) record.StagedConflictId = stagedConflictId;
        }
        else
        {
            var newRecord = new SyncInboxRecord
            {
                IdempotencyKey = idempotencyKey,
                Status = status,
                ProcessedAtUtc = processedAtUtc ?? (status == SyncInboxStatus.Applied ? DateTimeOffset.UtcNow : null),
                FailureReason = failureReason,
                StagedConflictId = stagedConflictId,
                ReceivedAtUtc = DateTimeOffset.UtcNow
            };
            _records[idempotencyKey] = newRecord;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RecordProcessedAsync(
        string idempotencyKey,
        Guid packetId,
        string entityKind,
        DateTimeOffset ingestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        if (_records.TryGetValue(idempotencyKey, out var existing))
        {
            existing.Status = SyncInboxStatus.Applied;
            existing.ProcessedAtUtc = ingestedAtUtc;
        }
        else
        {
            var record = new SyncInboxRecord
            {
                IdempotencyKey = idempotencyKey,
                PacketId = packetId,
                EntityKind = entityKind,
                Status = SyncInboxStatus.Applied,
                ReceivedAtUtc = ingestedAtUtc,
                ProcessedAtUtc = ingestedAtUtc
            };
            _records.TryAdd(idempotencyKey, record);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<int> GetProcessedCountAsync(CancellationToken cancellationToken = default)
    {
        var count = _records.Values.Count(r => r.Status == SyncInboxStatus.Applied);
        return Task.FromResult(count);
    }

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _records.Clear();
        return Task.CompletedTask;
    }
}
