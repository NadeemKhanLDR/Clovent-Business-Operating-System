using System.Text.Json;

namespace Clovent.Platform.Sync;

/// <summary>
/// Immutable envelope for delta synchronization packets exchanged between
/// POS terminals and branch replication hubs in a local-first topology.
/// </summary>
public sealed class SyncPacket
{
    /// <summary>Unique identifier for this specific packet instance.</summary>
    public Guid PacketId { get; set; } = Guid.NewGuid();

    /// <summary>Organization tenancy identifier scoping this replication stream.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Branch where this packet originated.</summary>
    public Guid SourceBranchId { get; set; }

    /// <summary>Terminal register that generated this change.</summary>
    public Guid SourceTerminalId { get; set; }

    /// <summary>Optional target branch for multi-site replication.</summary>
    public Guid? TargetBranchId { get; set; }

    /// <summary>Message schema version for forward/backward compatibility checks (default 1).</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>SHA-256 cryptographic digest of the inner serialized payload JSON to detect tampering or identity reuse.</summary>
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>Discriminator indicating the entity kind (e.g. InventoryStockDelta, CatalogPriceAdjustment).</summary>
    public string EntityKind { get; set; } = string.Empty;

    /// <summary>Unique identifier of the entity being replicated.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Operation semantics (Delta, Snapshot, Adjustment, Deactivation).</summary>
    public string Operation { get; set; } = SyncOperations.Delta;

    /// <summary>Strict concurrency token (e.g. RowVersion base64 or timestamp) for optimistic concurrency verification.</summary>
    public string? ConcurrencyToken { get; set; }

    /// <summary>UTC timestamp when the packet was generated at the source terminal.</summary>
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Terminal-scoped monotonic sequence number for ordering.</summary>
    public long SequenceNumber { get; set; }

    /// <summary>Idempotency key guaranteeing duplicate ingestion produces zero side-effects.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Serialized JSON payload carrying entity-specific delta details.</summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>Computes the SHA-256 hash hex string of the given text payload.</summary>
    public static string ComputeSha256(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Creates a generic synchronization packet.</summary>
    public static SyncPacket Create<TPayload>(
        Guid sourceBranchId,
        Guid sourceTerminalId,
        string entityKind,
        string entityId,
        string operation,
        TPayload payload,
        string? concurrencyToken = null,
        string? idempotencyKey = null,
        long sequenceNumber = 0,
        Guid? targetBranchId = null,
        Guid organizationId = default,
        int schemaVersion = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(payload);

        var packetId = Guid.NewGuid();
        var key = idempotencyKey ?? $"{sourceTerminalId}:{entityKind}:{entityId}:{packetId}";
        var json = JsonSerializer.Serialize(payload);
        var hash = ComputeSha256(json);

        return new SyncPacket
        {
            PacketId = packetId,
            OrganizationId = organizationId,
            SourceBranchId = sourceBranchId,
            SourceTerminalId = sourceTerminalId,
            TargetBranchId = targetBranchId,
            SchemaVersion = schemaVersion,
            PayloadHash = hash,
            EntityKind = entityKind,
            EntityId = entityId,
            Operation = operation,
            ConcurrencyToken = concurrencyToken,
            TimestampUtc = DateTimeOffset.UtcNow,
            SequenceNumber = sequenceNumber,
            IdempotencyKey = key,
            PayloadJson = json
        };
    }

    /// <summary>Deserializes the inner JSON payload into the specified type.</summary>
    public TPayload? DeserializePayload<TPayload>(JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(PayloadJson)) return default;
        return JsonSerializer.Deserialize<TPayload>(PayloadJson, options);
    }
}
