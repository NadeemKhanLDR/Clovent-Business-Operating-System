namespace Clovent.Platform.Sync;

/// <summary>Configuration options for authenticated HTTP transport between branch terminals.</summary>
public sealed class SyncTransportOptions
{
    /// <summary>Endpoint URI for the receiver HTTP listener or reverse proxy.</summary>
    public string EndpointUri { get; set; } = "http://localhost:14443/api/sync/deltas/";

    /// <summary>Shared cryptographic secret used for HMAC-SHA256 payload signing and tamper verification.</summary>
    public string SharedReplicationSecret { get; set; } = "CBOS_BRANCH_REPLICATION_SECRET_KEY_PROD_2026";

    /// <summary>Maximum allowed payload size in bytes to prevent denial of service (default 1 MB).</summary>
    public int MaxPayloadSizeBytes { get; set; } = 1024 * 1024;

    /// <summary>Network request timeout in seconds (default 10s).</summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>Maximum acceptable clock skew between sender and receiver in seconds (default 300s = 5m).</summary>
    public int AllowedClockSkewSeconds { get; set; } = 300;
}
