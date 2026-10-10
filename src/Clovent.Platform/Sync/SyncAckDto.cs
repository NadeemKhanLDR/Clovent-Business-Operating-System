namespace Clovent.Platform.Sync;

/// <summary>Receipt acknowledgement returned by the receiver only after durable commit.</summary>
public sealed class SyncAckDto
{
    /// <summary>Whether the packet was successfully accepted and durably persisted.</summary>
    public bool Success { get; set; }

    /// <summary>Delivered count.</summary>
    public int DeliveredCount { get; set; }

    /// <summary>Packet ID acknowledged.</summary>
    public Guid PacketId { get; set; }

    /// <summary>Final ingestion status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the packet was durably committed by the receiver.</summary>
    public DateTimeOffset IngestedAtUtc { get; set; }

    /// <summary>Optional error message if rejected or failed.</summary>
    public string? ErrorMessage { get; set; }
}
