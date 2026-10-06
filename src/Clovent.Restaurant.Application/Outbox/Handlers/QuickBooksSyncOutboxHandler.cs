using System.Text.Json;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.QuickBooks;
using Clovent.Restaurant.Outbox;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>
/// Asynchronously syncs completed sales to QuickBooks under circuit breaker protection.
/// Failures never block the cashier, and fast-fail backoff prevents hammering QuickBooks during an outage.
/// </summary>
public sealed class QuickBooksSyncOutboxHandler(
    IQuickBooksGateway quickBooksGateway,
    ICircuitBreakerRegistry circuitBreakerRegistry,
    ILogger<QuickBooksSyncOutboxHandler> logger) : IOutboxMessageHandler
{
    private readonly ICircuitBreaker _circuitBreaker = circuitBreakerRegistry.GetOrCreate("QuickBooks");

    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.QuickBooksSync;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<QuickBooksSyncPayload>(message.Payload)
            ?? throw new InvalidOperationException($"Invalid payload for QuickBooks message {message.Id}.");

        // Execute under circuit breaker protection
        var result = await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await quickBooksGateway.SyncOrderSaleAsync(payload, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException($"QuickBooks sync failed: {result.ErrorMessage}");
        }

        logger.LogInformation("Successfully synced order {OrderNumber} to QuickBooks (Ref: {ExternalId}).",
            payload.OrderNumber, result.ExternalTransactionId);
    }
}
