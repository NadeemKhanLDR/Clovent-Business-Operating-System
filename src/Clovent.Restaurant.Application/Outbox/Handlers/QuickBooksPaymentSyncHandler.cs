using System.Text.Json;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.QuickBooks;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.QuickBooks;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>
/// Asynchronously syncs customer payments to QuickBooks Desktop / Online under circuit breaker protection.
/// Links payments to previously synchronized invoices and enforces idempotent payment tracking.
/// </summary>
public sealed class QuickBooksPaymentSyncHandler : IOutboxMessageHandler
{
    private readonly IQuickBooksGateway _quickBooksGateway;
    private readonly IQuickBooksSyncMapRepository? _syncMapRepository;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ILogger<QuickBooksPaymentSyncHandler> _logger;

    /// <summary>Creates a new payment sync handler.</summary>
    public QuickBooksPaymentSyncHandler(
        IQuickBooksGateway quickBooksGateway,
        ICircuitBreakerRegistry circuitBreakerRegistry,
        ILogger<QuickBooksPaymentSyncHandler> logger,
        IQuickBooksSyncMapRepository? syncMapRepository = null)
    {
        _quickBooksGateway = quickBooksGateway ?? throw new ArgumentNullException(nameof(quickBooksGateway));
        _circuitBreaker = circuitBreakerRegistry?.GetOrCreate("QuickBooks") ?? throw new ArgumentNullException(nameof(circuitBreakerRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncMapRepository = syncMapRepository;
    }

    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.QuickBooksPaymentSync;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var request = JsonSerializer.Deserialize<QuickBooksPaymentRequest>(message.Payload)
            ?? throw new InvalidOperationException($"Invalid payload format for QuickBooks payment message {message.Id}.");

        // 1. Idempotency Check via Durable QuickBooksSyncMap
        QuickBooksSyncMap? paymentMap = null;
        if (_syncMapRepository != null)
        {
            paymentMap = await _syncMapRepository.GetByLocalEntityAsync(request.PaymentId, QuickBooksSyncEntityType.Payment, cancellationToken).ConfigureAwait(false);
            if (paymentMap != null && paymentMap.Status == QuickBooksSyncStatus.Synchronized)
            {
                _logger.LogInformation("Payment {PaymentId} is already synchronized to QuickBooks with TxnID {TxnId}. Skipping duplicate post.",
                    request.PaymentId, paymentMap.QuickBooksTxnId);
                return;
            }

            if (paymentMap == null)
            {
                paymentMap = QuickBooksSyncMap.Create(
                    localEntityId: request.PaymentId,
                    entityType: QuickBooksSyncEntityType.Payment,
                    amount: request.Amount,
                    currency: "USD",
                    requestPayloadJson: message.Payload);

                await _syncMapRepository.AddAsync(paymentMap, cancellationToken).ConfigureAwait(false);
            }

            // Link to previously synchronized invoice TxnID if not already set
            if (string.IsNullOrWhiteSpace(request.QuickBooksInvoiceTxnId))
            {
                var invoiceMap = await _syncMapRepository.GetByLocalEntityAsync(request.OrderId, QuickBooksSyncEntityType.Invoice, cancellationToken).ConfigureAwait(false);
                if (invoiceMap != null && !string.IsNullOrWhiteSpace(invoiceMap.QuickBooksTxnId))
                {
                    request = request with { QuickBooksInvoiceTxnId = invoiceMap.QuickBooksTxnId };
                }
            }
        }

        // 2. Transmit to QuickBooks Gateway under Circuit Breaker Protection
        QuickBooksSyncResult result;
        try
        {
            result = await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _quickBooksGateway.SyncPaymentAsync(request, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (paymentMap != null && _syncMapRepository != null)
            {
                paymentMap.MarkFailed(ex.Message);
                await _syncMapRepository.UpdateAsync(paymentMap, cancellationToken).ConfigureAwait(false);
            }
            throw;
        }

        if (!result.Success)
        {
            var err = result.ErrorMessage ?? "QuickBooks payment post failed.";
            if (paymentMap != null && _syncMapRepository != null)
            {
                paymentMap.MarkFailed(err);
                await _syncMapRepository.UpdateAsync(paymentMap, cancellationToken).ConfigureAwait(false);
            }
            throw new InvalidOperationException($"QuickBooks payment sync failed: {err}");
        }

        // 3. Record Durable Synchronization State
        if (paymentMap != null && _syncMapRepository != null)
        {
            paymentMap.MarkSynchronized(
                qbTxnId: result.ExternalTransactionId ?? $"QB-PAY-{request.PaymentId}",
                docNumber: result.DocumentNumber ?? $"PAY-{request.OrderNumber}",
                editSequence: result.EditSequence,
                responsePayloadJson: result.ResponsePayloadJson);

            await _syncMapRepository.UpdateAsync(paymentMap, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Successfully synchronized Payment {PaymentId} ({Method}: {Amount:C}) to QuickBooks (TxnID: {TxnId}).",
            request.PaymentId, request.PaymentMethod, request.Amount, result.ExternalTransactionId);
    }
}
