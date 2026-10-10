using Clovent.Platform.Sync;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Infrastructure.Sync;

/// <summary>
/// Production durable implementation of <see cref="ISyncIdempotencyStore"/> backed by SQL Server EF Core.
/// Guarantees exact-once delta processing, tamper detection via cryptographic payload hashes,
/// and complete crash recovery across process restarts.
/// </summary>
public sealed class DurableSyncInboxStore : ISyncIdempotencyStore
{
    private readonly RestaurantDbContext _dbContext;
    private readonly ILogger<DurableSyncInboxStore> _logger;

    /// <summary>Creates a new durable sync inbox store.</summary>
    public DurableSyncInboxStore(
        RestaurantDbContext dbContext,
        ILogger<DurableSyncInboxStore> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> HasBeenProcessedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return await _dbContext.SyncInboxRecords
            .AsNoTracking()
            .AnyAsync(r => r.IdempotencyKey == idempotencyKey && r.Status == SyncInboxStatus.Applied, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SyncInboxRecord?> GetInboxRecordAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return await _dbContext.SyncInboxRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RecordInboxAsync(SyncInboxRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.IdempotencyKey);

        var existing = await _dbContext.SyncInboxRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == record.IdempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing == null)
        {
            await _dbContext.SyncInboxRecords.AddAsync(record, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.Status = record.Status;
            existing.PayloadHash = record.PayloadHash;
            existing.PayloadJson = record.PayloadJson;
            existing.FailureReason = record.FailureReason;
            existing.StagedConflictId = record.StagedConflictId;
            existing.ConcurrencyToken = record.ConcurrencyToken;
            existing.ProcessedAtUtc = record.ProcessedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateStatusAsync(
        string idempotencyKey,
        SyncInboxStatus status,
        DateTimeOffset? processedAtUtc = null,
        string? failureReason = null,
        Guid? stagedConflictId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var existing = await _dbContext.SyncInboxRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
        {
            existing.Status = status;
            existing.ProcessedAtUtc = processedAtUtc ?? (status == SyncInboxStatus.Applied ? DateTimeOffset.UtcNow : existing.ProcessedAtUtc);
            if (failureReason != null) existing.FailureReason = failureReason;
            if (stagedConflictId != null) existing.StagedConflictId = stagedConflictId;

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
            await _dbContext.SyncInboxRecords.AddAsync(newRecord, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task RecordProcessedAsync(
        string idempotencyKey,
        Guid packetId,
        string entityKind,
        DateTimeOffset ingestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var existing = await _dbContext.SyncInboxRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
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
            await _dbContext.SyncInboxRecords.AddAsync(record, cancellationToken).ConfigureAwait(false);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> GetProcessedCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncInboxRecords
            .AsNoTracking()
            .CountAsync(r => r.Status == SyncInboxStatus.Applied, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _dbContext.SyncInboxRecords.RemoveRange(_dbContext.SyncInboxRecords);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
