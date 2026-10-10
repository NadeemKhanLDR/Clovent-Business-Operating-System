using System.Text.Json;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.QuickBooks;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.QuickBooks;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>
/// Asynchronously syncs completed sales invoices to QuickBooks Desktop / Online under circuit breaker protection.
/// Enforces idempotent reference tracking via <see cref="IQuickBooksSyncMapRepository"/> to prevent duplicate invoices.
/// </summary>
public sealed class QuickBooksInvoiceSyncHandler : IOutboxMessageHandler
{
    private readonly IQuickBooksGateway _quickBooksGateway;
    private readonly IQuickBooksSyncMapRepository? _syncMapRepository;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ILogger<QuickBooksInvoiceSyncHandler> _logger;

    /// <summary>Creates a new invoice sync handler.</summary>
    public QuickBooksInvoiceSyncHandler(
        IQuickBooksGateway quickBooksGateway,
        ICircuitBreakerRegistry circuitBreakerRegistry,
        ILogger<QuickBooksInvoiceSyncHandler> logger,
        IQuickBooksSyncMapRepository? syncMapRepository = null)
    {
        _quickBooksGateway = quickBooksGateway ?? throw new ArgumentNullException(nameof(quickBooksGateway));
        _circuitBreaker = circuitBreakerRegistry?.GetOrCreate("QuickBooks") ?? throw new ArgumentNullException(nameof(circuitBreakerRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncMapRepository = syncMapRepository;
    }

    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.QuickBooksInvoiceSync;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        QuickBooksInvoiceRequest? request = null;

        // Attempt deserialization from structured QuickBooksInvoiceRequest
        try
        {
            request = JsonSerializer.Deserialize<QuickBooksInvoiceRequest>(message.Payload);
        }
        catch
        {
            // Fallback: check if payload is legacy QuickBooksSyncPayload
        }

        if (request == null || request.Lines == null)
        {
            var fallback = JsonSerializer.Deserialize<QuickBooksSyncPayload>(message.Payload)
                ?? throw new InvalidOperationException($"Invalid payload format for QuickBooks invoice message {message.Id}.");

            request = new QuickBooksInvoiceRequest(
                OrderId: fallback.OrderId,
                OrderNumber: fallback.OrderNumber,
                CustomerName: fallback.CustomerName,
                SubTotal: fallback.TotalAmount,
                TaxAmount: 0m,
                DiscountAmount: 0m,
                TotalAmount: fallback.TotalAmount,
                Currency: "USD",
                TxnDate: fallback.CompletedAtUtc,
                Lines: Array.Empty<QuickBooksInvoiceLineItem>());
        }

        // 1. Idempotency Check via Durable QuickBooksSyncMap
        QuickBooksSyncMap? syncMap = null;
        if (_syncMapRepository != null)
        {
            syncMap = await _syncMapRepository.GetByLocalEntityAsync(request.OrderId, QuickBooksSyncEntityType.Invoice, cancellationToken).ConfigureAwait(false);
            if (syncMap != null && syncMap.Status == QuickBooksSyncStatus.Synchronized)
            {
                _logger.LogInformation("Order {OrderId} is already synchronized to QuickBooks with TxnID {TxnId}. Skipping duplicate post.",
                    request.OrderId, syncMap.QuickBooksTxnId);
                return;
            }

            if (syncMap == null)
            {
                syncMap = QuickBooksSyncMap.Create(
                    localEntityId: request.OrderId,
                    entityType: QuickBooksSyncEntityType.Invoice,
                    amount: request.TotalAmount,
                    currency: request.Currency,
                    branchId: request.BranchId,
                    terminalId: request.TerminalId,
                    requestPayloadJson: message.Payload);

                await _syncMapRepository.AddAsync(syncMap, cancellationToken).ConfigureAwait(false);
            }
        }

        // 2. Transmit to QuickBooks Gateway under Circuit Breaker Protection
        QuickBooksSyncResult result;
        try
        {
            result = await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _quickBooksGateway.SyncInvoiceAsync(request, cancellationToken).ConfigureAwait(false);
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
            var err = result.ErrorMessage ?? "QuickBooks invoice post failed.";
            if (syncMap != null && _syncMapRepository != null)
            {
                syncMap.MarkFailed(err);
                await _syncMapRepository.UpdateAsync(syncMap, cancellationToken).ConfigureAwait(false);
            }
            throw new InvalidOperationException($"QuickBooks invoice sync failed: {err}");
        }

        // 3. Record Durable Synchronization State
        if (syncMap != null && _syncMapRepository != null)
        {
            syncMap.MarkSynchronized(
                qbTxnId: result.ExternalTransactionId ?? $"QB-INV-{request.OrderId}",
                docNumber: result.DocumentNumber ?? request.OrderNumber,
                editSequence: result.EditSequence,
                responsePayloadJson: result.ResponsePayloadJson);

            await _syncMapRepository.UpdateAsync(syncMap, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Successfully synchronized Order {OrderNumber} to QuickBooks (TxnID: {TxnId}, DocNumber: {DocNumber}).",
            request.OrderNumber, result.ExternalTransactionId, result.DocumentNumber);
    }
}
