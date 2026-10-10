using System.Text.Json;
using Clovent.Platform.Sync;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Sync;

/// <summary>
/// Ingestion handler for <see cref="SyncEntityKinds.ShiftSummary"/>.
/// Replicates cashier shift drawer opening, closing, and cash variance data across the branch.
/// </summary>
public sealed class ShiftSummaryDeltaIngestionHandler : ISyncIngestionHandler
{
    private readonly IShiftSyncRegistry _shiftRegistry;
    private readonly ISyncConflictStagingStore _conflictStagingStore;
    private readonly ILogger<ShiftSummaryDeltaIngestionHandler> _logger;

    /// <summary>Creates a new shift summary delta ingestion handler.</summary>
    public ShiftSummaryDeltaIngestionHandler(
        IShiftSyncRegistry shiftRegistry,
        ISyncConflictStagingStore conflictStagingStore,
        ILogger<ShiftSummaryDeltaIngestionHandler> logger)
    {
        _shiftRegistry = shiftRegistry;
        _conflictStagingStore = conflictStagingStore;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string EntityKind => SyncEntityKinds.ShiftSummary;

    /// <inheritdoc/>
    public async Task<SyncIngestionResult> IngestAsync(SyncPacket packet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packet);

        var payload = packet.DeserializePayload<ShiftSummaryDeltaSyncPayload>();
        if (payload == null)
        {
            return SyncIngestionResult.Failed(packet, "Failed to deserialize ShiftSummaryDeltaSyncPayload.");
        }

        // Check if there is an existing shift with identical shift number but different cashier ID
        var existingShift = await _shiftRegistry.GetLatestTerminalShiftAsync(payload.BranchId, payload.TerminalId, cancellationToken).ConfigureAwait(false);
        if (existingShift != null && existingShift.ShiftNumber == payload.ShiftNumber && existingShift.CashierId != payload.CashierId)
        {
            var conflict = new SyncConflictRecord
            {
                PacketId = packet.PacketId,
                OriginalPacket = packet,
                EntityKind = EntityKind,
                EntityId = $"{payload.TerminalId}:{payload.ShiftNumber}",
                SourceBranchId = payload.BranchId,
                SourceTerminalId = payload.TerminalId,
                IncomingValue = $"Cashier: {payload.CashierName} ({payload.CashierId})",
                CurrentValue = $"Cashier: {existingShift.CashierName} ({existingShift.CashierId})",
                ConflictType = SyncConflictType.ConflictingShiftSession,
                ConflictReason = $"Shift #{payload.ShiftNumber} on terminal {payload.TerminalId} claimed by multiple cashiers.",
                DetectedAtUtc = DateTimeOffset.UtcNow
            };

            await _conflictStagingStore.StageConflictAsync(conflict, cancellationToken).ConfigureAwait(false);
            return SyncIngestionResult.StagedForReview(packet, conflict.ConflictId, conflict.ConflictReason);
        }

        await _shiftRegistry.RecordShiftSummaryAsync(payload, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Recorded shift #{ShiftNumber} for cashier {Cashier} on terminal {Terminal}.",
            payload.ShiftNumber, payload.CashierName, payload.TerminalId);

        return SyncIngestionResult.Applied(packet, $"Recorded shift #{payload.ShiftNumber} summary for cashier {payload.CashierName}.");
    }
}
