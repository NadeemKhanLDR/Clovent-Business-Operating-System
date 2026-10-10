namespace Clovent.Platform.Sync;

/// <summary>Status outcome of processing an incoming sync delta packet.</summary>
public enum SyncIngestionStatus
{
    /// <summary>Delta was successfully and atomically applied to the local database.</summary>
    Applied,

    /// <summary>Packet was previously ingested; duplicate processing skipped idempotently.</summary>
    AlreadyProcessed,

    /// <summary>Conflicting edit was detected and staged for manager review.</summary>
    StagedForManagerReview,

    /// <summary>Incoming change was rejected because local state is newer (LWW superseded).</summary>
    RejectedStale,

    /// <summary>Incoming change was rejected due to scope mismatch, tampering, identity reuse, or policy violation.</summary>
    Rejected,

    /// <summary>Ingestion failed due to unrecoverable validation or domain error.</summary>
    Failed
}

/// <summary>Result returned after ingesting a sync packet.</summary>
public sealed record SyncIngestionResult(
    Guid PacketId,
    string EntityKind,
    string EntityId,
    SyncIngestionStatus Status,
    string? Message = null,
    Guid? StagedConflictId = null)
{
    /// <summary>Creates an Applied result.</summary>
    public static SyncIngestionResult Applied(SyncPacket packet, string? message = null)
        => new(packet.PacketId, packet.EntityKind, packet.EntityId, SyncIngestionStatus.Applied, message);

    /// <summary>Creates an AlreadyProcessed idempotent result.</summary>
    public static SyncIngestionResult AlreadyProcessed(SyncPacket packet)
        => new(packet.PacketId, packet.EntityKind, packet.EntityId, SyncIngestionStatus.AlreadyProcessed, "Packet already ingested.");

    /// <summary>Creates a SelfLoopSuppressed result for origin terminal loopback prevention.</summary>
    public static SyncIngestionResult SelfLoopSuppressed(SyncPacket packet)
        => new(packet.PacketId, packet.EntityKind, packet.EntityId, SyncIngestionStatus.AlreadyProcessed, "Origin terminal self-loop suppressed.");

    /// <summary>Creates a StagedForManagerReview result.</summary>
    public static SyncIngestionResult StagedForReview(SyncPacket packet, Guid conflictId, string reason)
        => new(packet.PacketId, packet.EntityKind, packet.EntityId, SyncIngestionStatus.StagedForManagerReview, reason, conflictId);

    /// <summary>Creates a RejectedStale result.</summary>
    public static SyncIngestionResult RejectedStale(SyncPacket packet, string reason)
        => new(packet.PacketId, packet.EntityKind, packet.EntityId, SyncIngestionStatus.RejectedStale, reason);

    /// <summary>Creates a Rejected result for security, tampering, identity reuse, or cross-org rejection.</summary>
    public static SyncIngestionResult Rejected(SyncPacket packet, string reason)
        => new(packet.PacketId, packet.EntityKind, packet.EntityId, SyncIngestionStatus.Rejected, reason);

    /// <summary>Creates a Failed result.</summary>
    public static SyncIngestionResult Failed(SyncPacket packet, string error)
        => new(packet.PacketId, packet.EntityKind, packet.EntityId, SyncIngestionStatus.Failed, error);
}

/// <summary>Handler interface for processing entity-specific incoming delta synchronization packets.</summary>
public interface ISyncIngestionHandler
{
    /// <summary>The entity kind this handler processes (e.g. InventoryStockDelta, CatalogPriceAdjustment).</summary>
    string EntityKind { get; }

    /// <summary>Processes the incoming sync packet applying delta or staging conflict.</summary>
    Task<SyncIngestionResult> IngestAsync(SyncPacket packet, CancellationToken cancellationToken = default);
}

/// <summary>Ingestion engine coordinating idempotent routing and conflict management for incoming sync packets.</summary>
public interface ISyncIngestionEngine
{
    /// <summary>Ingests a single packet idempotently.</summary>
    Task<SyncIngestionResult> IngestAsync(SyncPacket packet, CancellationToken cancellationToken = default);

    /// <summary>Ingests a batch of packets preserving ordering and transaction boundaries.</summary>
    Task<IReadOnlyList<SyncIngestionResult>> IngestBatchAsync(IReadOnlyList<SyncPacket> packets, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a staged conflict according to store manager review decision,
    /// optionally reapplying the edit under manager elevation and updating idempotency history.
    /// </summary>
    Task<SyncIngestionResult> ResolveStagedConflictAsync(
        Guid conflictId,
        SyncConflictStatus decision,
        Guid reviewerUserId,
        string? notes = null,
        CancellationToken cancellationToken = default);
}
