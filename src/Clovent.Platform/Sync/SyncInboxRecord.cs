namespace Clovent.Platform.Sync;

/// <summary>
/// Processing state for an ingested delta-sync packet in the durable inbox.
/// Enables explicit distinction between receipt, staging for human review,
/// committed application, and terminal rejection.
/// </summary>
public enum SyncInboxStatus
{
    /// <summary>Packet has been durably received and staged in the inbox pending processing.</summary>
    Received,

    /// <summary>Packet encountered an operational discrepancy or concurrency collision and is staged for authorized manager review.</summary>
    StagedForReview,

    /// <summary>Packet domain effects have been durably applied and committed to the authoritative ledger.</summary>
    Applied,

    /// <summary>Packet was rejected due to scope mismatch, tampering, identity reuse, or unrecoverable error.</summary>
    Rejected
}

/// <summary>
/// Durable inbox record guaranteeing exact-once processing, identity verification,
/// tamper detection, and crash recovery across process and database restarts.
/// </summary>
public sealed class SyncInboxRecord
{
    /// <summary>Terminal-scoped idempotency key uniquely identifying the logical event.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Unique identifier of the packet instance.</summary>
    public Guid PacketId { get; set; }

    /// <summary>Organization tenancy identifier.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Branch where the packet originated.</summary>
    public Guid BranchId { get; set; }

    /// <summary>Terminal that generated the packet.</summary>
    public Guid SourceTerminalId { get; set; }

    /// <summary>Discriminator indicating the entity kind.</summary>
    public string EntityKind { get; set; } = string.Empty;

    /// <summary>Target entity primary key / identifier.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Schema version of the packet envelope.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>SHA-256 hash of the payload at receipt time to reject identity reuse with altered contents.</summary>
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>Serialized raw payload for auditability, reconciliation, and staged replay.</summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>Durable processing status.</summary>
    public SyncInboxStatus Status { get; set; } = SyncInboxStatus.Received;

    /// <summary>UTC timestamp when the packet was durably recorded in the receiver inbox.</summary>
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>UTC timestamp when the packet was applied or resolved.</summary>
    public DateTimeOffset? ProcessedAtUtc { get; set; }

    /// <summary>Diagnostic error or reason if rejected or staged for review.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Associated conflict ID if staged for manager review.</summary>
    public Guid? StagedConflictId { get; set; }

    /// <summary>Concurrency token of the target entity at ingestion time.</summary>
    public string? ConcurrencyToken { get; set; }
}
