using System.Text.Json;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.QuickBooks;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.QuickBooks;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>
/// Asynchronously syncs daily shift sales and cash drawer summaries to QuickBooks as general journal entries.
/// </summary>
public sealed class QuickBooksShiftSyncHandler : IOutboxMessageHandler
{
    private readonly IQuickBooksGateway _quickBooksGateway;
    private readonly IQuickBooksSyncMapRepository? _syncMapRepository;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ILogger<QuickBooksShiftSyncHandler> _logger;

    /// <summary>Creates a new shift sync handler.</summary>
    public QuickBooksShiftSyncHandler(
        IQuickBooksGateway quickBooksGateway,
        ICircuitBreakerRegistry circuitBreakerRegistry,
        ILogger<QuickBooksShiftSyncHandler> logger,
        IQuickBooksSyncMapRepository? syncMapRepository = null)
    {
        _quickBooksGateway = quickBooksGateway ?? throw new ArgumentNullException(nameof(quickBooksGateway));
        _circuitBreaker = circuitBreakerRegistry?.GetOrCreate("QuickBooks") ?? throw new ArgumentNullException(nameof(circuitBreakerRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncMapRepository = syncMapRepository;
    }

    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.QuickBooksShiftSync;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        QuickBooksShiftSummaryRequest? request = null;
        try
        {
            request = JsonSerializer.Deserialize<QuickBooksShiftSummaryRequest>(message.Payload);
        }
        catch
        {
            // Fallback
        }

        if (request == null)
        {
            var fallback = JsonSerializer.Deserialize<ShiftSummaryDeltaSyncPayload>(message.Payload)
                ?? throw new InvalidOperationException($"Invalid payload format for QuickBooks shift summary message {message.Id}.");

            request = new QuickBooksShiftSummaryRequest(
                ShiftId: Guid.Parse(message.AggregateId),
                ShiftNumber: fallback.ShiftNumber,
                TerminalId: fallback.TerminalId,
                CashierName: fallback.CashierName,
                TotalSales: fallback.NetSales,
                CashTendered: fallback.CountedCash,
                CashVariance: fallback.CashVariance,
                OrderCount: fallback.TotalOrdersCount,
                ClosedAtUtc: fallback.ClosedAtUtc ?? DateTimeOffset.UtcNow);
        }

        // 1. Idempotency Check via Durable QuickBooksSyncMap
        QuickBooksSyncMap? shiftMap = null;
        if (_syncMapRepository != null)
        {
            shiftMap = await _syncMapRepository.GetByLocalEntityAsync(request.ShiftId, QuickBooksSyncEntityType.ShiftSummary, cancellationToken).ConfigureAwait(false);
            if (shiftMap != null && shiftMap.Status == QuickBooksSyncStatus.Synchronized)
            {
                _logger.LogInformation("Shift {ShiftNumber} ({ShiftId}) is already synchronized to QuickBooks with TxnID {TxnId}. Skipping duplicate post.",
                    request.ShiftNumber, request.ShiftId, shiftMap.QuickBooksTxnId);
                return;
            }

            if (shiftMap == null)
            {
                shiftMap = QuickBooksSyncMap.Create(
                    localEntityId: request.ShiftId,
                    entityType: QuickBooksSyncEntityType.ShiftSummary,
                    amount: request.TotalSales,
                    currency: "USD",
                    terminalId: request.TerminalId,
                    requestPayloadJson: message.Payload);

                await _syncMapRepository.AddAsync(shiftMap, cancellationToken).ConfigureAwait(false);
            }
        }

        // 2. Transmit to QuickBooks Gateway under Circuit Breaker Protection
        QuickBooksSyncResult result;
        try
        {
            result = await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _quickBooksGateway.SyncShiftSalesSummaryAsync(request, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (shiftMap != null && _syncMapRepository != null)
            {
                shiftMap.MarkFailed(ex.Message);
                await _syncMapRepository.UpdateAsync(shiftMap, cancellationToken).ConfigureAwait(false);
            }
            throw;
        }

        if (!result.Success)
        {
            var err = result.ErrorMessage ?? "QuickBooks shift summary post failed.";
            if (shiftMap != null && _syncMapRepository != null)
            {
                shiftMap.MarkFailed(err);
                await _syncMapRepository.UpdateAsync(shiftMap, cancellationToken).ConfigureAwait(false);
            }
            throw new InvalidOperationException($"QuickBooks shift sync failed: {err}");
        }

        // 3. Record Durable Synchronization State
        if (shiftMap != null && _syncMapRepository != null)
        {
            shiftMap.MarkSynchronized(
                qbTxnId: result.ExternalTransactionId ?? $"QB-SHIFT-{request.ShiftId}",
                docNumber: result.DocumentNumber ?? $"SHIFT-{request.ShiftNumber}",
                editSequence: result.EditSequence,
                responsePayloadJson: result.ResponsePayloadJson);

            await _syncMapRepository.UpdateAsync(shiftMap, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Successfully synchronized Shift #{ShiftNumber} (Sales: {Sales:C}) to QuickBooks (TxnID: {TxnId}).",
            request.ShiftNumber, request.TotalSales, result.ExternalTransactionId);
    }
}
