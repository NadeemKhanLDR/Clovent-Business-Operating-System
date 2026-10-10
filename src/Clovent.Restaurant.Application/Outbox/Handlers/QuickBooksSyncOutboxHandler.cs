using System.Text.Json;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.QuickBooks;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.QuickBooks;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>
/// Asynchronously syncs completed sales to QuickBooks under circuit breaker protection.
/// Failures never block the cashier, and fast-fail backoff prevents hammering QuickBooks during an outage.
/// When <see cref="IQuickBooksSyncMapRepository"/> is available, persists durable sync mappings for idempotency.
/// </summary>
public sealed class QuickBooksSyncOutboxHandler : IOutboxMessageHandler
{
    private readonly IQuickBooksGateway _quickBooksGateway;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ILogger<QuickBooksSyncOutboxHandler> _logger;
    private readonly IQuickBooksSyncMapRepository? _syncMapRepository;

    /// <summary>Constructor supporting optional sync map repository for backwards compatibility with test harnesses.</summary>
    public QuickBooksSyncOutboxHandler(
        IQuickBooksGateway quickBooksGateway,
        ICircuitBreakerRegistry circuitBreakerRegistry,
        ILogger<QuickBooksSyncOutboxHandler> logger,
        IQuickBooksSyncMapRepository? syncMapRepository = null)
    {
        _quickBooksGateway = quickBooksGateway ?? throw new ArgumentNullException(nameof(quickBooksGateway));
        _circuitBreaker = circuitBreakerRegistry?.GetOrCreate("QuickBooks") ?? throw new ArgumentNullException(nameof(circuitBreakerRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncMapRepository = syncMapRepository;
    }

    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.QuickBooksSync;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<QuickBooksSyncPayload>(message.Payload)
            ?? throw new InvalidOperationException($"Invalid payload for QuickBooks message {message.Id}.");

        // Idempotency check via mapping ledger if available
        QuickBooksSyncMap? syncMap = null;
        if (_syncMapRepository != null)
        {
            syncMap = await _syncMapRepository.GetByLocalEntityAsync(payload.OrderId, QuickBooksSyncEntityType.Invoice, cancellationToken).ConfigureAwait(false);
            if (syncMap != null && syncMap.Status == QuickBooksSyncStatus.Synchronized)
            {
                _logger.LogInformation("Order {OrderId} is already synchronized to QuickBooks with TxnID {TxnId}. Skipping duplicate post.",
                    payload.OrderId, syncMap.QuickBooksTxnId);
                return;
            }

            if (syncMap == null)
            {
                syncMap = QuickBooksSyncMap.Create(
                    localEntityId: payload.OrderId,
                    entityType: QuickBooksSyncEntityType.Invoice,
                    amount: payload.TotalAmount,
                    currency: "USD",
                    requestPayloadJson: message.Payload);

                await _syncMapRepository.AddAsync(syncMap, cancellationToken).ConfigureAwait(false);
            }
        }

        // Execute under circuit breaker protection
        QuickBooksSyncResult result;
        try
        {
            result = await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _quickBooksGateway.SyncOrderSaleAsync(payload, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (syncMap != null && _syncMapRepository != null)
            {
                syncMap.MarkFailed(ex.Message);
                await _syncMapRepository.UpdateAsync(syncMap, cancellationToken).ConfigureAwait(false);
            }
            throw;
        }

        if (!result.Success)
        {
            var err = result.ErrorMessage ?? "QuickBooks sync failed.";
            if (syncMap != null && _syncMapRepository != null)
            {
                syncMap.MarkFailed(err);
                await _syncMapRepository.UpdateAsync(syncMap, cancellationToken).ConfigureAwait(false);
            }
            throw new InvalidOperationException($"QuickBooks sync failed: {err}");
        }

        if (syncMap != null && _syncMapRepository != null)
        {
            syncMap.MarkSynchronized(
                qbTxnId: result.ExternalTransactionId ?? $"QB-INV-{payload.OrderId}",
                docNumber: result.DocumentNumber ?? payload.OrderNumber,
                editSequence: result.EditSequence,
                responsePayloadJson: result.ResponsePayloadJson);

            await _syncMapRepository.UpdateAsync(syncMap, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Successfully synced order {OrderNumber} to QuickBooks (Ref: {ExternalId}).",
            payload.OrderNumber, result.ExternalTransactionId);
    }
}
