using System.Collections.Concurrent;
using System.Text.Json;

namespace Clovent.Platform.Sync;

/// <summary>Categorization of replication and delta-sync conflicts.</summary>
public enum SyncConflictType
{
    /// <summary>Concurrency token (RowVersion) on the target entity did not match the expected baseline.</summary>
    ConcurrencyTokenMismatch,

    /// <summary>Two concurrent terminals adjusted prices within overlapping effective windows.</summary>
    OverlappingPriceEdit,

    /// <summary>Relative or absolute stock delta resulted in negative stock or unexpected deviation.</summary>
    NegativeStockDiscrepancy,

    /// <summary>Delta packet timestamp is older than current committed state (stale update).</summary>
    StaleTimestampConflict,

    /// <summary>Conflicting shift numbers or overlapping cashier sessions.</summary>
    ConflictingShiftSession
}

/// <summary>Lifecycle resolution state for a staged sync conflict.</summary>
public enum SyncConflictStatus
{
    /// <summary>Conflict is staged and awaiting back-office store manager review.</summary>
    PendingReview,

    /// <summary>Manager or engine approved automatic Last-Write-Wins (LWW) resolution.</summary>
    ApprovedManagerLww,

    /// <summary>Manager manually approved the incoming edit, overriding local state.</summary>
    ApprovedManagerOverride,

    /// <summary>Manager rejected the incoming edit, preserving local state.</summary>
    RejectedManager
}

/// <summary>
/// A conflict detected during multi-terminal delta synchronization, staged for auditability
/// and authorized store manager review to prevent silent ledger or pricing corruption.
/// </summary>
public sealed class SyncConflictRecord
{
    /// <summary>Unique identifier for this conflict incident.</summary>
    public Guid ConflictId { get; set; } = Guid.NewGuid();

    /// <summary>ID of the synchronization packet that caused the conflict.</summary>
    public Guid PacketId { get; set; }

    /// <summary>The original sync packet that caused the conflict, staged for manager replay.</summary>
    public SyncPacket? OriginalPacket { get; set; }

    /// <summary>Serialized JSON payload of the original packet for durable storage across process restarts.</summary>
    public string OriginalPacketJson { get; set; } = string.Empty;

    /// <summary>Entity kind (InventoryStockDelta, CatalogPriceAdjustment, etc.).</summary>
    public string EntityKind { get; set; } = string.Empty;

    /// <summary>Entity primary key / identifier.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Branch where the conflicting change originated.</summary>
    public Guid SourceBranchId { get; set; }

    /// <summary>Terminal that submitted the conflicting change.</summary>
    public Guid SourceTerminalId { get; set; }

    /// <summary>Incoming proposed value (e.g. price amount, stock quantity).</summary>
    public string IncomingValue { get; set; } = string.Empty;

    /// <summary>Current target value in the local database.</summary>
    public string CurrentValue { get; set; } = string.Empty;

    /// <summary>Incoming concurrency token or RowVersion.</summary>
    public string? IncomingConcurrencyToken { get; set; }

    /// <summary>Current target concurrency token or RowVersion.</summary>
    public string? CurrentConcurrencyToken { get; set; }

    /// <summary>Classification of the conflict.</summary>
    public SyncConflictType ConflictType { get; set; }

    /// <summary>Current resolution status.</summary>
    public SyncConflictStatus Status { get; set; } = SyncConflictStatus.PendingReview;

    /// <summary>Human-readable diagnostic reason explaining why the conflict occurred.</summary>
    public string ConflictReason { get; set; } = string.Empty;

    /// <summary>Timestamp when the conflict was detected.</summary>
    public DateTimeOffset DetectedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Timestamp when the conflict was reviewed and resolved.</summary>
    public DateTimeOffset? ReviewedAtUtc { get; set; }

    /// <summary>User ID of the reviewing store manager.</summary>
    public Guid? ReviewedByUserId { get; set; }

    /// <summary>Optional resolution notes or audit explanation.</summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>Recorded resulting domain effect applied upon manager resolution.</summary>
    public string? ResultingEffect { get; set; }

    /// <summary>Deserializes the staged original packet if available.</summary>
    public SyncPacket? GetOriginalPacket()
    {
        if (OriginalPacket != null) return OriginalPacket;
        if (string.IsNullOrWhiteSpace(OriginalPacketJson)) return null;
        return JsonSerializer.Deserialize<SyncPacket>(OriginalPacketJson);
    }
}

/// <summary>Contract for persisting and querying staged synchronization conflicts.</summary>
public interface ISyncConflictStagingStore
{
    /// <summary>Stages a new conflict record for manager review.</summary>
    Task StageConflictAsync(SyncConflictRecord conflict, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all unresolved conflicts awaiting manager review.</summary>
    Task<IReadOnlyList<SyncConflictRecord>> GetPendingConflictsAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves a conflict record by its ID.</summary>
    Task<SyncConflictRecord?> GetConflictByIdAsync(Guid conflictId, CancellationToken cancellationToken = default);

    /// <summary>Resolves a conflict with a specific decision and records reviewer audit details.</summary>
    Task ResolveConflictAsync(
        Guid conflictId,
        SyncConflictStatus resolution,
        Guid reviewerUserId,
        string? notes = null,
        string? resultingEffect = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the count of pending conflicts awaiting review.</summary>
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}

/// <summary>Thread-safe in-memory staging store for sync conflicts (strictly test harness).</summary>
public sealed class InMemorySyncConflictStagingStore : ISyncConflictStagingStore
{
    private readonly ConcurrentDictionary<Guid, SyncConflictRecord> _conflicts = new();

    /// <inheritdoc/>
    public Task StageConflictAsync(SyncConflictRecord conflict, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        if (conflict.OriginalPacket != null && string.IsNullOrWhiteSpace(conflict.OriginalPacketJson))
        {
            conflict.OriginalPacketJson = JsonSerializer.Serialize(conflict.OriginalPacket);
        }
        _conflicts.TryAdd(conflict.ConflictId, conflict);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncConflictRecord>> GetPendingConflictsAsync(CancellationToken cancellationToken = default)
    {
        var pending = _conflicts.Values
            .Where(c => c.Status == SyncConflictStatus.PendingReview)
            .OrderByDescending(c => c.DetectedAtUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<SyncConflictRecord>>(pending);
    }

    /// <inheritdoc/>
    public Task<SyncConflictRecord?> GetConflictByIdAsync(Guid conflictId, CancellationToken cancellationToken = default)
    {
        _conflicts.TryGetValue(conflictId, out var conflict);
        return Task.FromResult(conflict);
    }

    /// <inheritdoc/>
    public Task ResolveConflictAsync(
        Guid conflictId,
        SyncConflictStatus resolution,
        Guid reviewerUserId,
        string? notes = null,
        string? resultingEffect = null,
        CancellationToken cancellationToken = default)
    {
        if (_conflicts.TryGetValue(conflictId, out var conflict))
        {
            conflict.Status = resolution;
            conflict.ReviewedByUserId = reviewerUserId;
            conflict.ReviewedAtUtc = DateTimeOffset.UtcNow;
            conflict.ResolutionNotes = notes;
            conflict.ResultingEffect = resultingEffect;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        var count = _conflicts.Values.Count(c => c.Status == SyncConflictStatus.PendingReview);
        return Task.FromResult(count);
    }
}
