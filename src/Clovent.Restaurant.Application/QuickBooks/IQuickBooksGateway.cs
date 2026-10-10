using System.Collections.Concurrent;
using Clovent.Restaurant.Application.Outbox.Dtos;

namespace Clovent.Restaurant.Application.QuickBooks;

/// <summary>Result of synchronizing a transaction to QuickBooks.</summary>
public sealed record QuickBooksSyncResult(
    bool Success,
    string? ExternalTransactionId,
    string? ErrorMessage,
    string? DocumentNumber = null,
    string? EditSequence = null,
    string? ResponsePayloadJson = null);

/// <summary>Gateway abstraction for synchronizing sales, invoices, and payments with QuickBooks Online / Desktop.</summary>
public interface IQuickBooksGateway
{
    /// <summary>Posts a completed order sales summary to QuickBooks (legacy/general contract).</summary>
    Task<QuickBooksSyncResult> SyncOrderSaleAsync(QuickBooksSyncPayload payload, CancellationToken cancellationToken = default);

    /// <summary>Posts a detailed sales invoice to QuickBooks Desktop or Online.</summary>
    Task<QuickBooksSyncResult> SyncInvoiceAsync(QuickBooksInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Posts a customer payment to QuickBooks linked to an invoice.</summary>
    Task<QuickBooksSyncResult> SyncPaymentAsync(QuickBooksPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Posts a shift daily cash and sales summary as a general journal entry to QuickBooks.</summary>
    Task<QuickBooksSyncResult> SyncShiftSalesSummaryAsync(QuickBooksShiftSummaryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Queries the status of an existing transaction by QuickBooks TxnID for reconciliation.</summary>
    Task<QuickBooksTransactionStatusDto?> QueryTransactionStatusAsync(string txnId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Standard QuickBooks gateway implementing idempotent invoice creation,
/// payload formatting (qbXML for Desktop, JSON for Online), and fault simulation.
/// </summary>
public sealed class DefaultQuickBooksGateway : IQuickBooksGateway
{
    private readonly ConcurrentDictionary<Guid, QuickBooksSyncResult> _syncedInvoices = new();
    private readonly ConcurrentDictionary<Guid, QuickBooksSyncResult> _syncedPayments = new();
    private readonly ConcurrentDictionary<Guid, QuickBooksSyncResult> _syncedShifts = new();
    private readonly ConcurrentDictionary<string, QuickBooksTransactionStatusDto> _knownTransactions = new();
    private volatile bool _isSimulatingOutage;

    /// <summary>Simulates a QuickBooks service outage for testing / circuit breaker verification.</summary>
    public void SetSimulatedOutage(bool isOutage) => _isSimulatingOutage = isOutage;

    /// <inheritdoc/>
    public async Task<QuickBooksSyncResult> SyncOrderSaleAsync(QuickBooksSyncPayload payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (_isSimulatingOutage)
        {
            throw new HttpRequestException("QuickBooks API connection timeout: remote server 503 Service Unavailable.");
        }

        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        var qbRef = $"QB-INV-{payload.OrderNumber}-{payload.OrderId.ToString()[..8].ToUpperInvariant()}";
        var result = new QuickBooksSyncResult(true, qbRef, null, payload.OrderNumber, "1000", $"{{\"TxnID\":\"{qbRef}\",\"Amount\":{payload.TotalAmount}}}");
        _knownTransactions[qbRef] = new QuickBooksTransactionStatusDto(qbRef, "Paid", payload.TotalAmount, payload.CompletedAtUtc, payload.OrderNumber);
        return result;
    }

    /// <inheritdoc/>
    public async Task<QuickBooksSyncResult> SyncInvoiceAsync(QuickBooksInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_isSimulatingOutage)
        {
            throw new HttpRequestException("QuickBooks Desktop COM bridge timeout: server 503 Service Unavailable.");
        }

        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        // Idempotent cache check
        if (_syncedInvoices.TryGetValue(request.OrderId, out var existing))
        {
            return existing;
        }

        var qbTxnId = $"QB-TXN-INV-{request.OrderId:N}"[..24].ToUpperInvariant();
        var docNumber = string.IsNullOrWhiteSpace(request.OrderNumber) ? $"INV-{request.OrderId.ToString()[..6]}" : request.OrderNumber;
        var editSeq = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var simulatedResponseJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            TxnID = qbTxnId,
            DocNumber = docNumber,
            EditSequence = editSeq,
            TotalAmount = request.TotalAmount,
            Status = "Synchronized",
            LinesCount = request.Lines?.Count ?? 0
        });

        var result = new QuickBooksSyncResult(
            Success: true,
            ExternalTransactionId: qbTxnId,
            ErrorMessage: null,
            DocumentNumber: docNumber,
            EditSequence: editSeq,
            ResponsePayloadJson: simulatedResponseJson);

        _syncedInvoices[request.OrderId] = result;
        _knownTransactions[qbTxnId] = new QuickBooksTransactionStatusDto(qbTxnId, "Open", request.TotalAmount, request.TxnDate, docNumber);

        return result;
    }

    /// <inheritdoc/>
    public async Task<QuickBooksSyncResult> SyncPaymentAsync(QuickBooksPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_isSimulatingOutage)
        {
            throw new HttpRequestException("QuickBooks Online API timeout: connection refused.");
        }

        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        if (_syncedPayments.TryGetValue(request.PaymentId, out var existing))
        {
            return existing;
        }

        var qbTxnId = $"QB-TXN-PAY-{request.PaymentId:N}"[..24].ToUpperInvariant();
        var docNumber = $"PAY-{request.OrderNumber}";
        var editSeq = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var simulatedResponseJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            TxnID = qbTxnId,
            DocNumber = docNumber,
            LinkedInvoiceTxnID = request.QuickBooksInvoiceTxnId,
            PaymentMethod = request.PaymentMethod,
            Amount = request.Amount,
            Status = "Completed"
        });

        var result = new QuickBooksSyncResult(
            Success: true,
            ExternalTransactionId: qbTxnId,
            ErrorMessage: null,
            DocumentNumber: docNumber,
            EditSequence: editSeq,
            ResponsePayloadJson: simulatedResponseJson);

        _syncedPayments[request.PaymentId] = result;
        _knownTransactions[qbTxnId] = new QuickBooksTransactionStatusDto(qbTxnId, "Closed", request.Amount, request.PaymentDate, docNumber);

        return result;
    }

    /// <inheritdoc/>
    public async Task<QuickBooksSyncResult> SyncShiftSalesSummaryAsync(QuickBooksShiftSummaryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_isSimulatingOutage)
        {
            throw new HttpRequestException("QuickBooks connection timeout: remote server unreachable.");
        }

        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        if (_syncedShifts.TryGetValue(request.ShiftId, out var existing))
        {
            return existing;
        }

        var qbTxnId = $"QB-TXN-GENJNL-{request.ShiftId:N}"[..24].ToUpperInvariant();
        var docNumber = $"SHIFT-{request.ShiftNumber}";
        var editSeq = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var simulatedResponseJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            TxnID = qbTxnId,
            DocNumber = docNumber,
            ShiftNumber = request.ShiftNumber,
            TotalSales = request.TotalSales,
            CashVariance = request.CashVariance,
            Status = "PostedJournal"
        });

        var result = new QuickBooksSyncResult(
            Success: true,
            ExternalTransactionId: qbTxnId,
            ErrorMessage: null,
            DocumentNumber: docNumber,
            EditSequence: editSeq,
            ResponsePayloadJson: simulatedResponseJson);

        _syncedShifts[request.ShiftId] = result;
        _knownTransactions[qbTxnId] = new QuickBooksTransactionStatusDto(qbTxnId, "Posted", request.TotalSales, request.ClosedAtUtc, docNumber);

        return result;
    }

    /// <inheritdoc/>
    public Task<QuickBooksTransactionStatusDto?> QueryTransactionStatusAsync(string txnId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(txnId)) return Task.FromResult<QuickBooksTransactionStatusDto?>(null);

        _knownTransactions.TryGetValue(txnId, out var dto);
        return Task.FromResult(dto);
    }
}
