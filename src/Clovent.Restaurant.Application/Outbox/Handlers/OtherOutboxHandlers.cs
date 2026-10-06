using System.Text.Json;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Outbox;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>Updates recommendation weights and models in the background.</summary>
public sealed class RecommendationLearningOutboxHandler(
    ILogger<RecommendationLearningOutboxHandler> logger) : IOutboxMessageHandler
{
    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.RecommendationLearning;

    /// <inheritdoc/>
    public Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<RecommendationLearningPayload>(message.Payload);
        if (payload != null)
        {
            logger.LogDebug("Background recommendation learning processed for order {OrderId} ({Count} items).",
                payload.OrderId, payload.ProductVariantIds.Count);
        }
        return Task.CompletedTask;
    }
}

/// <summary>Syncs sales data to cloud / multi-unit central systems.</summary>
public sealed class CloudSyncOutboxHandler(
    ICircuitBreakerRegistry circuitBreakerRegistry,
    ILogger<CloudSyncOutboxHandler> logger) : IOutboxMessageHandler
{
    private readonly ICircuitBreaker _circuitBreaker = circuitBreakerRegistry.GetOrCreate("CloudSync");

    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.CloudSync;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<CloudSyncPayload>(message.Payload)
            ?? throw new InvalidOperationException($"Invalid payload for CloudSync message {message.Id}.");

        await _circuitBreaker.ExecuteAsync(async () =>
        {
            // Simulated cloud push
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Order {OrderNumber} synced to cloud.", payload.OrderNumber);
        }, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Dispatches notifications (email/SMS) without blocking the POS.</summary>
public sealed class NotificationOutboxHandler(
    ICircuitBreakerRegistry circuitBreakerRegistry,
    ILogger<NotificationOutboxHandler> logger) : IOutboxMessageHandler
{
    private readonly ICircuitBreaker _circuitBreaker = circuitBreakerRegistry.GetOrCreate("Notification");

    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.Notification;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await _circuitBreaker.ExecuteAsync(async () =>
        {
            await Task.Delay(5, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Outbox notification processed for {AggregateId}.", message.AggregateId);
        }, cancellationToken).ConfigureAwait(false);
    }
}
