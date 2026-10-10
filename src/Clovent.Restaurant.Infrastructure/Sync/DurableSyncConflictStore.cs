using System.Text.Json;
using Clovent.Platform.Sync;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Infrastructure.Sync;

/// <summary>
/// Production durable implementation of <see cref="ISyncConflictStagingStore"/> backed by SQL Server EF Core.
/// Staging conflicts with complete payload serialization allows store managers to review and resolve
/// conflicting edits durably, surviving workstation reboots and application restarts.
/// </summary>
public sealed class DurableSyncConflictStore : ISyncConflictStagingStore
{
    private readonly RestaurantDbContext _dbContext;
    private readonly ILogger<DurableSyncConflictStore> _logger;

    /// <summary>Creates a new durable sync conflict store.</summary>
    public DurableSyncConflictStore(
        RestaurantDbContext dbContext,
        ILogger<DurableSyncConflictStore> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task StageConflictAsync(SyncConflictRecord conflict, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        if (conflict.OriginalPacket != null && string.IsNullOrWhiteSpace(conflict.OriginalPacketJson))
        {
            conflict.OriginalPacketJson = JsonSerializer.Serialize(conflict.OriginalPacket);
        }

        await _dbContext.SyncConflicts.AddAsync(conflict, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Durably staged sync conflict {ConflictId} ({EntityKind}:{EntityId}) under status {Status}.",
            conflict.ConflictId, conflict.EntityKind, conflict.EntityId, conflict.Status);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncConflictRecord>> GetPendingConflictsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncConflicts
            .AsNoTracking()
            .Where(c => c.Status == SyncConflictStatus.PendingReview)
            .OrderByDescending(c => c.DetectedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SyncConflictRecord?> GetConflictByIdAsync(Guid conflictId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncConflicts
            .FirstOrDefaultAsync(c => c.ConflictId == conflictId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ResolveConflictAsync(
        Guid conflictId,
        SyncConflictStatus resolution,
        Guid reviewerUserId,
        string? notes = null,
        string? resultingEffect = null,
        CancellationToken cancellationToken = default)
    {
        var conflict = await _dbContext.SyncConflicts
            .FirstOrDefaultAsync(c => c.ConflictId == conflictId, cancellationToken)
            .ConfigureAwait(false);

        if (conflict == null)
        {
            _logger.LogWarning("Cannot resolve non-existent sync conflict {ConflictId}.", conflictId);
            return;
        }

        conflict.Status = resolution;
        conflict.ReviewedByUserId = reviewerUserId;
        conflict.ReviewedAtUtc = DateTimeOffset.UtcNow;
        conflict.ResolutionNotes = notes;
        conflict.ResultingEffect = resultingEffect;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Durably resolved sync conflict {ConflictId} to {Resolution} by user {UserId}.",
            conflictId, resolution, reviewerUserId);
    }

    /// <inheritdoc/>
    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncConflicts
            .AsNoTracking()
            .CountAsync(c => c.Status == SyncConflictStatus.PendingReview, cancellationToken)
            .ConfigureAwait(false);
    }
}
