using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Clovent.Platform.Sync;

/// <summary>
/// Core ingestion engine enforcing idempotent packet processing, identity verification,
/// tamper detection, cross-organization rejection, self-loop suppression, and routing to entity handlers.
/// </summary>
public sealed class SyncIngestionEngine : ISyncIngestionEngine
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _inboxLocks = new();
    private readonly Dictionary<string, ISyncIngestionHandler> _handlers;
    private readonly ISyncIdempotencyStore _idempotencyStore;
    private readonly ISyncConflictStagingStore? _conflictStagingStore;
    private readonly SyncScopeContext _scopeContext;
    private readonly ILogger<SyncIngestionEngine> _logger;

    /// <summary>Creates a new sync ingestion engine.</summary>
    public SyncIngestionEngine(
        IEnumerable<ISyncIngestionHandler> handlers,
        ISyncIdempotencyStore idempotencyStore,
        ILogger<SyncIngestionEngine> logger,
        ISyncConflictStagingStore? conflictStagingStore = null,
        SyncScopeContext? scopeContext = null)
    {
        _handlers = handlers.ToDictionary(h => h.EntityKind, h => h, StringComparer.OrdinalIgnoreCase);
        _idempotencyStore = idempotencyStore;
        _conflictStagingStore = conflictStagingStore;
        _scopeContext = scopeContext ?? new SyncScopeContext();
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SyncIngestionResult> IngestAsync(SyncPacket packet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packet);

        // 1. Origin Terminal Self-Loop Suppression
        if (_scopeContext.TerminalId != Guid.Empty && packet.SourceTerminalId == _scopeContext.TerminalId)
        {
            _logger.LogInformation("Packet {PacketId} origin matches local terminal ({TerminalId}). Self-loop suppressed without duplicate application.",
                packet.PacketId, packet.SourceTerminalId);
            return SyncIngestionResult.SelfLoopSuppressed(packet);
        }

        // 2. Cross-Organization Tenancy Boundary Check
        if (_scopeContext.OrganizationId != Guid.Empty &&
            packet.OrganizationId != Guid.Empty &&
            packet.OrganizationId != _scopeContext.OrganizationId)
        {
            _logger.LogWarning("Packet {PacketId} rejected: Cross-organization tenancy boundary violation (Packet Org={PacketOrg}, Local Org={LocalOrg}).",
                packet.PacketId, packet.OrganizationId, _scopeContext.OrganizationId);
            return SyncIngestionResult.Rejected(packet,
                $"Cross-organization replication prohibited. Packet belongs to organization {packet.OrganizationId}, host is {_scopeContext.OrganizationId}.");
        }

        // 3. Schema Version Verification
        if (packet.SchemaVersion > _scopeContext.SupportedSchemaVersion)
        {
            _logger.LogWarning("Packet {PacketId} rejected: Unsupported schema version {Version} (Supported <= {MaxVersion}).",
                packet.PacketId, packet.SchemaVersion, _scopeContext.SupportedSchemaVersion);
            return SyncIngestionResult.Rejected(packet,
                $"Unsupported envelope schema version {packet.SchemaVersion}. Max supported version is {_scopeContext.SupportedSchemaVersion}.");
        }

        // Ensure payload hash is populated
        if (string.IsNullOrWhiteSpace(packet.PayloadHash))
        {
            packet.PayloadHash = SyncPacket.ComputeSha256(packet.PayloadJson);
        }

        // 4. Strict Durable Inbox & Tamper / Identity Reuse Check with Concurrency Gate
        var gate = _inboxLocks.GetOrAdd(packet.IdempotencyKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existingRecord = await _idempotencyStore.GetInboxRecordAsync(packet.IdempotencyKey, cancellationToken).ConfigureAwait(false);
            if (existingRecord != null)
            {
                // Identity reuse check: does payload content match?
                if (!string.IsNullOrWhiteSpace(existingRecord.PayloadHash) &&
                    !string.IsNullOrWhiteSpace(packet.PayloadHash) &&
                    !string.Equals(existingRecord.PayloadHash, packet.PayloadHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Security violation: Identity reuse rejected for packet {PacketId} with key '{Key}'. Payload content differs from previously recorded hash.",
                        packet.PacketId, packet.IdempotencyKey);
                    return SyncIngestionResult.Rejected(packet,
                        $"Reuse of event identity '{packet.IdempotencyKey}' with conflicting payload content is strictly prohibited.");
                }

                if (existingRecord.Status == SyncInboxStatus.Applied)
                {
                    _logger.LogDebug("Duplicate sync packet {PacketId} with key '{Key}' already Applied. Skipping.",
                        packet.PacketId, packet.IdempotencyKey);
                    return SyncIngestionResult.AlreadyProcessed(packet);
                }

                if (existingRecord.Status == SyncInboxStatus.StagedForReview)
                {
                    _logger.LogDebug("Duplicate sync packet {PacketId} with key '{Key}' is currently StagedForReview.",
                        packet.PacketId, packet.IdempotencyKey);
                    return SyncIngestionResult.StagedForReview(packet, existingRecord.StagedConflictId ?? Guid.Empty,
                        existingRecord.FailureReason ?? "Staged for manager review.");
                }

                if (existingRecord.Status == SyncInboxStatus.Rejected)
                {
                    _logger.LogDebug("Duplicate sync packet {PacketId} with key '{Key}' was previously Rejected.",
                        packet.PacketId, packet.IdempotencyKey);
                    return SyncIngestionResult.Rejected(packet, existingRecord.FailureReason ?? "Packet previously rejected.");
                }
            }

            // 5. Record initial receipt in durable inbox
            var inboxRecord = new SyncInboxRecord
            {
                IdempotencyKey = packet.IdempotencyKey,
                PacketId = packet.PacketId,
                OrganizationId = packet.OrganizationId,
                BranchId = packet.SourceBranchId,
                SourceTerminalId = packet.SourceTerminalId,
                EntityKind = packet.EntityKind,
                EntityId = packet.EntityId,
                SchemaVersion = packet.SchemaVersion,
                PayloadHash = packet.PayloadHash,
                PayloadJson = packet.PayloadJson,
                Status = SyncInboxStatus.Received,
                ReceivedAtUtc = DateTimeOffset.UtcNow,
                ConcurrencyToken = packet.ConcurrencyToken
            };
            await _idempotencyStore.RecordInboxAsync(inboxRecord, cancellationToken).ConfigureAwait(false);

            // 6. Resolve Handler
            if (!_handlers.TryGetValue(packet.EntityKind, out var handler))
            {
                _logger.LogWarning("No sync ingestion handler registered for entity kind '{EntityKind}'.", packet.EntityKind);
                var error = $"No handler registered for '{packet.EntityKind}'.";
                await _idempotencyStore.UpdateStatusAsync(packet.IdempotencyKey, SyncInboxStatus.Rejected, failureReason: error, cancellationToken: cancellationToken).ConfigureAwait(false);
                return SyncIngestionResult.Failed(packet, error);
            }

            // 7. Delegate to Entity Handler
            try
            {
                var result = await handler.IngestAsync(packet, cancellationToken).ConfigureAwait(false);

                if (result.Status == SyncIngestionStatus.Applied)
                {
                    await _idempotencyStore.UpdateStatusAsync(
                        packet.IdempotencyKey,
                        SyncInboxStatus.Applied,
                        processedAtUtc: DateTimeOffset.UtcNow,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("Successfully ingested delta sync packet {PacketId} for {EntityKind}:{EntityId}.",
                        packet.PacketId, packet.EntityKind, packet.EntityId);
                }
                else if (result.Status == SyncIngestionStatus.StagedForManagerReview)
                {
                    await _idempotencyStore.UpdateStatusAsync(
                        packet.IdempotencyKey,
                        SyncInboxStatus.StagedForReview,
                        failureReason: result.Message,
                        stagedConflictId: result.StagedConflictId,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    _logger.LogWarning("Sync packet {PacketId} staged for manager review due to conflict on {EntityKind}:{EntityId}.",
                        packet.PacketId, packet.EntityKind, packet.EntityId);
                }
                else if (result.Status == SyncIngestionStatus.Rejected || result.Status == SyncIngestionStatus.RejectedStale)
                {
                    await _idempotencyStore.UpdateStatusAsync(
                        packet.IdempotencyKey,
                        SyncInboxStatus.Rejected,
                        failureReason: result.Message,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while ingesting sync packet {PacketId} ({EntityKind}).", packet.PacketId, packet.EntityKind);
                await _idempotencyStore.UpdateStatusAsync(
                    packet.IdempotencyKey,
                    SyncInboxStatus.Rejected,
                    failureReason: ex.Message,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return SyncIngestionResult.Failed(packet, ex.Message);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncIngestionResult>> IngestBatchAsync(IReadOnlyList<SyncPacket> packets, CancellationToken cancellationToken = default)
    {
        if (packets == null || packets.Count == 0) return Array.Empty<SyncIngestionResult>();

        var results = new List<SyncIngestionResult>(packets.Count);
        foreach (var packet in packets)
        {
            results.Add(await IngestAsync(packet, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<SyncIngestionResult> ResolveStagedConflictAsync(
        Guid conflictId,
        SyncConflictStatus decision,
        Guid reviewerUserId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (_conflictStagingStore == null)
        {
            throw new InvalidOperationException("No ISyncConflictStagingStore configured for SyncIngestionEngine.");
        }

        if (reviewerUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Resolution requires an authenticated reviewer user identifier.");
        }

        var conflict = await _conflictStagingStore.GetConflictByIdAsync(conflictId, cancellationToken).ConfigureAwait(false);
        if (conflict == null)
        {
            return new SyncIngestionResult(Guid.Empty, string.Empty, string.Empty, SyncIngestionStatus.Failed, "Conflict record not found.");
        }

        if (conflict.Status != SyncConflictStatus.PendingReview)
        {
            return new SyncIngestionResult(conflict.PacketId, conflict.EntityKind, conflict.EntityId, SyncIngestionStatus.Failed,
                $"Conflict {conflictId} has already been resolved with status {conflict.Status}.");
        }

        if (decision == SyncConflictStatus.RejectedManager)
        {
            await _conflictStagingStore.ResolveConflictAsync(conflictId, decision, reviewerUserId, notes, "Rejected by manager.", cancellationToken).ConfigureAwait(false);
            var packet = conflict.GetOriginalPacket();
            if (packet != null)
            {
                await _idempotencyStore.UpdateStatusAsync(
                    packet.IdempotencyKey,
                    SyncInboxStatus.Rejected,
                    failureReason: notes ?? "Conflict rejected by store manager.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Manager {User} rejected conflict {ConflictId} ({EntityKind}:{EntityId}).",
                reviewerUserId, conflictId, conflict.EntityKind, conflict.EntityId);

            return new SyncIngestionResult(conflict.PacketId, conflict.EntityKind, conflict.EntityId, SyncIngestionStatus.AlreadyProcessed, "Conflict rejected by manager.");
        }

        if (decision == SyncConflictStatus.ApprovedManagerLww || decision == SyncConflictStatus.ApprovedManagerOverride)
        {
            var packetToReplay = conflict.GetOriginalPacket();
            if (packetToReplay == null)
            {
                await _conflictStagingStore.ResolveConflictAsync(conflictId, decision, reviewerUserId, notes, "Approved without packet replay.", cancellationToken).ConfigureAwait(false);
                return new SyncIngestionResult(conflict.PacketId, conflict.EntityKind, conflict.EntityId, SyncIngestionStatus.Applied, "Approved without replaying packet.");
            }

            if (!_handlers.TryGetValue(packetToReplay.EntityKind, out var handler))
            {
                return SyncIngestionResult.Failed(packetToReplay, $"No handler registered for '{packetToReplay.EntityKind}'.");
            }

            // Force override token so entity handler bypasses standard collision check while rechecking stale approval
            packetToReplay.ConcurrencyToken = "*";

            var applyResult = await handler.IngestAsync(packetToReplay, cancellationToken).ConfigureAwait(false);
            if (applyResult.Status == SyncIngestionStatus.Applied)
            {
                var effectSummary = applyResult.Message ?? "Approved and applied to local domain state.";
                await _conflictStagingStore.ResolveConflictAsync(conflictId, decision, reviewerUserId, notes, effectSummary, cancellationToken).ConfigureAwait(false);
                await _idempotencyStore.UpdateStatusAsync(
                    packetToReplay.IdempotencyKey,
                    SyncInboxStatus.Applied,
                    processedAtUtc: DateTimeOffset.UtcNow,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Manager {User} approved and applied conflict {ConflictId} ({EntityKind}:{EntityId}). Effect: {Effect}",
                    reviewerUserId, conflictId, conflict.EntityKind, conflict.EntityId, effectSummary);
            }

            return applyResult;
        }

        return new SyncIngestionResult(conflict.PacketId, conflict.EntityKind, conflict.EntityId, SyncIngestionStatus.Failed, "Unsupported conflict resolution decision.");
    }
}
