using System.Text.Json;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Sync;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Outbox;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>
/// Common helper converting dispatch failures into circuit-breaker or backoff exceptions.
/// </summary>
internal static class DeltaSyncDispatchHelper
{
    public static async Task ExecuteDispatchAsync(
        ISyncPacketDispatcher dispatcher,
        SyncPacket packet,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.DispatchPacketAsync(packet, cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            if (result.IsOffline)
            {
                logger.LogDebug("Network is offline during delta-sync push of packet {PacketId}: {Error}",
                    packet.PacketId, result.ErrorMessage);
                throw new NetworkOfflineException(result.ErrorMessage ?? "Network is offline. Deferred for autonomous push.");
            }

            if (result.CircuitOpen)
            {
                logger.LogWarning("Circuit breaker open during delta-sync push of packet {PacketId}: {Error}",
                    packet.PacketId, result.ErrorMessage);
                throw new CircuitBreakerOpenException(SyncCircuitBreakerNames.BranchDeltaSync, TimeSpan.FromSeconds(30), result.ErrorMessage);
            }

            logger.LogWarning("Delta-sync dispatch failed for packet {PacketId}: {Error}", packet.PacketId, result.ErrorMessage);
            throw new InvalidOperationException(result.ErrorMessage ?? "Delta-sync dispatch failed.");
        }
    }
}

/// <summary>Outbox handler for generic <see cref="OutboxMessageType.DeltaSyncPacket"/> envelopes.</summary>
public sealed class DeltaSyncPacketOutboxHandler(
    ISyncPacketDispatcher dispatcher,
    ILogger<DeltaSyncPacketOutboxHandler> logger) : IOutboxMessageHandler
{
    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.DeltaSyncPacket;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        SyncPacket packet;
        try
        {
            var envelope = JsonSerializer.Deserialize<DeltaSyncPacketEnvelopePayload>(message.Payload);
            packet = envelope?.Packet ?? JsonSerializer.Deserialize<SyncPacket>(message.Payload)
                ?? throw new InvalidOperationException("Failed to deserialize SyncPacket from payload.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Corrupt payload in outbox message {message.Id}: {ex.Message}", ex);
        }

        await DeltaSyncDispatchHelper.ExecuteDispatchAsync(dispatcher, packet, logger, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Outbox handler pushing local inventory stock deltas to replication targets.</summary>
public sealed class InventoryDeltaSyncOutboxHandler(
    ISyncPacketDispatcher dispatcher,
    ILogger<InventoryDeltaSyncOutboxHandler> logger) : IOutboxMessageHandler
{
    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.InventoryDeltaSync;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<InventoryDeltaSyncPayload>(message.Payload)
            ?? throw new InvalidOperationException($"Invalid payload for InventoryDeltaSync {message.Id}.");

        var packet = SyncPacket.Create(
            sourceBranchId: payload.BranchId,
            sourceTerminalId: payload.TerminalId,
            entityKind: SyncEntityKinds.InventoryStockDelta,
            entityId: payload.ProductVariantId.ToString(),
            operation: payload.OperationType,
            payload: payload,
            concurrencyToken: payload.ConcurrencyToken,
            idempotencyKey: message.IdempotencyKey ?? $"inv-delta:{payload.TerminalId}:{payload.ProductVariantId}:{message.Id}");

        await DeltaSyncDispatchHelper.ExecuteDispatchAsync(dispatcher, packet, logger, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Outbox handler pushing catalog price adjustments to replication targets.</summary>
public sealed class CatalogPriceDeltaSyncOutboxHandler(
    ISyncPacketDispatcher dispatcher,
    ILogger<CatalogPriceDeltaSyncOutboxHandler> logger) : IOutboxMessageHandler
{
    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.CatalogPriceDeltaSync;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<CatalogPriceAdjustmentPayload>(message.Payload)
            ?? throw new InvalidOperationException($"Invalid payload for CatalogPriceDeltaSync {message.Id}.");

        var packet = SyncPacket.Create(
            sourceBranchId: payload.BranchId,
            sourceTerminalId: payload.TerminalId,
            entityKind: SyncEntityKinds.CatalogPriceAdjustment,
            entityId: payload.ProductVariantId.ToString(),
            operation: SyncOperations.Adjustment,
            payload: payload,
            concurrencyToken: payload.ConcurrencyToken,
            idempotencyKey: message.IdempotencyKey ?? $"price-delta:{payload.TerminalId}:{payload.ProductVariantId}:{message.Id}");

        await DeltaSyncDispatchHelper.ExecuteDispatchAsync(dispatcher, packet, logger, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Outbox handler pushing cashier shift summaries to replication targets.</summary>
public sealed class ShiftSummaryDeltaSyncOutboxHandler(
    ISyncPacketDispatcher dispatcher,
    ILogger<ShiftSummaryDeltaSyncOutboxHandler> logger) : IOutboxMessageHandler
{
    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.ShiftSummaryDeltaSync;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<ShiftSummaryDeltaSyncPayload>(message.Payload)
            ?? throw new InvalidOperationException($"Invalid payload for ShiftSummaryDeltaSync {message.Id}.");

        var packet = SyncPacket.Create(
            sourceBranchId: payload.BranchId,
            sourceTerminalId: payload.TerminalId,
            entityKind: SyncEntityKinds.ShiftSummary,
            entityId: $"{payload.BranchId}:{payload.TerminalId}:{payload.ShiftNumber}",
            operation: SyncOperations.Snapshot,
            payload: payload,
            idempotencyKey: message.IdempotencyKey ?? $"shift-delta:{payload.TerminalId}:{payload.ShiftNumber}:{message.Id}");

        await DeltaSyncDispatchHelper.ExecuteDispatchAsync(dispatcher, packet, logger, cancellationToken).ConfigureAwait(false);
    }
}
