using System.Text.Json;
using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Tables;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Continuity;

/// <summary>
/// Replays transactions recorded locally in offline Continuity Mode into the primary database,
/// enforcing strict idempotency and cryptographic integrity checks.
/// </summary>
public sealed class EmergencyJournalReplayer : IEmergencyJournalReplayer
{
    private readonly IContinuityJournalStore _journalStore;
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IOutboxRepository? _outboxRepository;
    private readonly IOutboxProcessor? _outboxProcessor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmergencyJournalReplayer>? _logger;

    /// <summary>Initializes a new instance of <see cref="EmergencyJournalReplayer"/>.</summary>
    public EmergencyJournalReplayer(
        IContinuityJournalStore journalStore,
        IOrderRepository orderRepository,
        IOrderLineRepository orderLineRepository,
        IPaymentRepository paymentRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IUnitOfWork unitOfWork,
        IOutboxRepository? outboxRepository = null,
        IOutboxProcessor? outboxProcessor = null,
        ILogger<EmergencyJournalReplayer>? logger = null)
    {
        _journalStore = journalStore;
        _orderRepository = orderRepository;
        _orderLineRepository = orderLineRepository;
        _paymentRepository = paymentRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _outboxProcessor = outboxProcessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EmergencyReplayResult> ReplayPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _journalStore.GetPendingReplayAsync(cancellationToken).ConfigureAwait(false);
        if (pending.Count == 0)
        {
            return new EmergencyReplayResult(0, 0, 0, 0, Array.Empty<string>());
        }

        var successCount = 0;
        var duplicateCount = 0;
        var failedCount = 0;
        var errors = new List<string>();

        var paymentMethods = await _paymentMethodRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var cashMethod = paymentMethods.FirstOrDefault(p => p.Name.Value.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                         ?? paymentMethods.FirstOrDefault();

        if (cashMethod == null)
        {
            const string msg = "Cannot replay emergency transactions: No active Cash payment method exists in the database.";
            _logger?.LogError(msg);
            return new EmergencyReplayResult(pending.Count, 0, 0, pending.Count, new[] { msg });
        }

        foreach (var tx in pending)
        {
            var idempotencyKey = $"emergency:{tx.TransactionId}";
            try
            {
                // Step 1: Verify cryptographic checksum and HMAC signature
                if (!tx.VerifyChecksum() || string.IsNullOrWhiteSpace(tx.HmacSignature) || !tx.VerifyHmacSignature())
                {
                    tx.ReconciliationStatus = ReconciliationStatus.Conflict;
                    tx.ReconciliationDetails = "Tamper check failed: Cryptographic checksum or HMAC signature does not match transaction payload.";
                    await _journalStore.UpdateAsync(tx, cancellationToken).ConfigureAwait(false);
                    failedCount++;
                    errors.Add($"Transaction {tx.TransactionId} rejected: Cryptographic verification mismatch.");
                    continue;
                }

                // Step 2: Idempotency check against existing payments
                var existingPayment = await _paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
                if (existingPayment != null)
                {
                    tx.ReconciliationStatus = ReconciliationStatus.DuplicateIgnored;
                    tx.ReconciliationDetails = $"Transaction already exists in primary database as Payment {existingPayment.Id.Value}.";
                    tx.ReconciledAtUtc = DateTimeOffset.UtcNow;
                    await _journalStore.UpdateAsync(tx, cancellationToken).ConfigureAwait(false);
                    duplicateCount++;
                    continue;
                }

                // Step 3: Reconstruct Order and lines
                var orderType = Enum.TryParse<OrderType>(tx.OrderSnapshot.OrderType, true, out var parsedType)
                    ? parsedType
                    : OrderType.TakeAway;

                TableId? tableId = (orderType == OrderType.DineIn && tx.OrderSnapshot.TableId.HasValue)
                    ? new TableId(tx.OrderSnapshot.TableId.Value)
                    : null;

                var order = Order.Create(
                    orderType,
                    new WarehouseId(tx.WarehouseId),
                    tableId,
                    OrderNumber.Generate(tx.TimestampUtc));

                var effectiveNotes = string.IsNullOrWhiteSpace(tx.LocalReceiptNumber)
                    ? tx.OrderSnapshot.Notes
                    : (string.IsNullOrWhiteSpace(tx.OrderSnapshot.Notes)
                        ? $"[Continuity Receipt: {tx.LocalReceiptNumber}]"
                        : $"[Continuity Receipt: {tx.LocalReceiptNumber}] {tx.OrderSnapshot.Notes}");

                if (!string.IsNullOrWhiteSpace(effectiveNotes))
                {
                    order.SetNotes(effectiveNotes);
                }
                if (!string.IsNullOrWhiteSpace(tx.OrderSnapshot.CustomerNotes))
                {
                    order.SetCustomerNotes(tx.OrderSnapshot.CustomerNotes);
                }

                foreach (var lineSnapshot in tx.OrderSnapshot.Lines)
                {
                    var orderLine = OrderLine.Create(
                        order.Id,
                        new ProductVariantId(lineSnapshot.ProductVariantId),
                        lineSnapshot.Quantity,
                        lineSnapshot.UnitPrice,
                        lineSnapshot.TaxRatePercentage,
                        lineSnapshot.TaxIsInclusive,
                        lineSnapshot.Notes);

                    await _orderLineRepository.AddAsync(orderLine, cancellationToken).ConfigureAwait(false);
                    order.AddOrderLine(orderLine.Id);
                }

                // Step 4: Record Payment
                var payment = Payment.Create(
                    order.Id,
                    cashMethod.Id,
                    tx.OrderSnapshot.GrandTotal,
                    null,
                    idempotencyKey);

                await _paymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
                order.RecordPayment(payment.Id);

                // Step 5: Complete order and create immutable receipt snapshot
                order.Complete();

                var snapshotItems = tx.OrderSnapshot.Lines.Select(l =>
                    new ReceiptSnapshotItem(
                        l.ProductVariantId,
                        l.Sku,
                        l.Name,
                        l.Quantity,
                        l.UnitPrice,
                        l.LineTotal,
                        l.Notes,
                        l.TaxRatePercentage > 0m ? "Taxable" : "Exempt",
                        $"PK-TAX-{l.TaxRatePercentage:0.##}",
                        l.TaxRatePercentage,
                        l.TaxIsInclusive,
                        l.TaxIsInclusive ? l.LineTotal - l.TaxAmount : l.LineTotal,
                        l.TaxAmount,
                        l.DiscountAmount)).ToList();

                var snapshotPayments = new List<ReceiptSnapshotPayment>
                {
                    new(tx.OrderSnapshot.GrandTotal, "Cash")
                };

                var receiptSnapshot = new ReceiptSnapshot(
                    order.Id.Value,
                    order.OrderNumber.Value,
                    order.DailySalesNumber,
                    order.OrderType.ToString(),
                    tx.TimestampUtc,
                    snapshotItems,
                    tx.OrderSnapshot.Subtotal,
                    tx.OrderSnapshot.TaxTotal,
                    tx.OrderSnapshot.DiscountTotal,
                    tx.OrderSnapshot.ServiceChargeTotal,
                    tx.OrderSnapshot.GrandTotal,
                    tx.OrderSnapshot.RoundingAmount,
                    snapshotPayments,
                    tx.CashierName,
                    tx.TerminalId,
                    tx.OrderSnapshot.CustomerNotes,
                    null,
                    "1.3.0-AwayFromZero-v1",
                    tx.OrderSnapshot.Subtotal,
                    tx.OrderSnapshot.Lines.Where(l => !l.TaxIsInclusive).Sum(l => l.TaxAmount),
                    tx.OrderSnapshot.Lines.Where(l => l.TaxIsInclusive).Sum(l => l.TaxAmount));

                order.SetReceiptSnapshot(JsonSerializer.Serialize(receiptSnapshot));
                await _orderRepository.AddAsync(order, cancellationToken).ConfigureAwait(false);

                // Step 6: Enqueue Outbox messages atomically
                if (_outboxRepository != null)
                {
                    var invPayload = JsonSerializer.Serialize(new
                    {
                        OrderId = order.Id.Value,
                        WarehouseId = tx.WarehouseId,
                        Lines = tx.OrderSnapshot.Lines.Select(l => new { l.ProductVariantId, l.Quantity }).ToList()
                    });
                    await _outboxRepository.AddAsync(OutboxMessage.Create(
                        OutboxMessageType.InventoryPosting,
                        "Order",
                        order.Id.Value.ToString(),
                        order.Id.Value.ToString(),
                        invPayload,
                        $"inv:{order.Id.Value}"), cancellationToken).ConfigureAwait(false);

                    var qbPayload = JsonSerializer.Serialize(new
                    {
                        OrderId = order.Id.Value,
                        OrderNumber = order.OrderNumber.Value,
                        TotalAmount = tx.OrderSnapshot.GrandTotal,
                        CustomerName = tx.CashierName,
                        CompletedAtUtc = tx.TimestampUtc
                    });
                    await _outboxRepository.AddAsync(OutboxMessage.Create(
                        OutboxMessageType.QuickBooksSync,
                        "Order",
                        order.Id.Value.ToString(),
                        order.Id.Value.ToString(),
                        qbPayload,
                        $"qb:{order.Id.Value}"), cancellationToken).ConfigureAwait(false);

                    var cloudPayload = JsonSerializer.Serialize(new
                    {
                        OrderId = order.Id.Value,
                        OrderNumber = order.OrderNumber.Value,
                        GrandTotal = tx.OrderSnapshot.GrandTotal,
                        SyncType = "OrderCompleted"
                    });
                    await _outboxRepository.AddAsync(OutboxMessage.Create(
                        OutboxMessageType.CloudSync,
                        "Order",
                        order.Id.Value.ToString(),
                        order.Id.Value.ToString(),
                        cloudPayload,
                        $"cloud:{order.Id.Value}"), cancellationToken).ConfigureAwait(false);

                    var analyticsPayload = JsonSerializer.Serialize(new
                    {
                        EventName = "OrderCompletedOfflineReplayed",
                        OrderId = order.Id.Value,
                        Total = tx.OrderSnapshot.GrandTotal,
                        ItemCount = tx.OrderSnapshot.Lines.Count,
                        ReplayedAtUtc = DateTimeOffset.UtcNow
                    });
                    await _outboxRepository.AddAsync(OutboxMessage.Create(
                        OutboxMessageType.AnalyticsEvent,
                        "Order",
                        order.Id.Value.ToString(),
                        order.Id.Value.ToString(),
                        analyticsPayload,
                        $"analytics:{order.Id.Value}"), cancellationToken).ConfigureAwait(false);
                }

                // Step 7: Commit primary DB transaction
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                // Step 8: Update local journal record
                tx.ReconciliationStatus = ReconciliationStatus.Replayed;
                tx.ReconciliationDetails = $"Replayed successfully as Order {order.OrderNumber.Value} ({order.Id.Value})" +
                    (string.IsNullOrWhiteSpace(tx.LocalReceiptNumber) ? string.Empty : $" [Local: {tx.LocalReceiptNumber}]");
                tx.ReconciledAtUtc = DateTimeOffset.UtcNow;
                await _journalStore.UpdateAsync(tx, cancellationToken).ConfigureAwait(false);

                successCount++;
                _logger?.LogInformation("Successfully replayed emergency transaction {TransactionId} as Order {OrderNumber}",
                    tx.TransactionId, order.OrderNumber.Value);
            }
            catch (Exception ex)
            {
                // Concurrency & idempotency check: did a concurrent replay thread already commit this payment?
                try
                {
                    var committedPayment = await _paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
                    if (committedPayment != null)
                    {
                        tx.ReconciliationStatus = ReconciliationStatus.DuplicateIgnored;
                        tx.ReconciliationDetails = $"Transaction already committed by concurrent replay as Payment {committedPayment.Id.Value}.";
                        tx.ReconciledAtUtc = DateTimeOffset.UtcNow;
                        await _journalStore.UpdateAsync(tx, cancellationToken).ConfigureAwait(false);
                        duplicateCount++;
                        _logger?.LogInformation("Concurrent replay race resolved for {TransactionId}; successfully recovered as duplicate ignored.", tx.TransactionId);
                        continue;
                    }
                }
                catch (Exception recoveryEx)
                {
                    _logger?.LogWarning(recoveryEx, "Failed idempotency recovery check for transaction {TransactionId}", tx.TransactionId);
                }

                failedCount++;
                errors.Add($"Failed to replay {tx.TransactionId}: {ex.Message}");
                _logger?.LogError(ex, "Failed to replay emergency transaction {TransactionId}", tx.TransactionId);

                tx.ReconciliationStatus = ReconciliationStatus.RequiresManagerReview;
                tx.ReconciliationDetails = $"Replay exception: {ex.Message}";
                await _journalStore.UpdateAsync(tx, cancellationToken).ConfigureAwait(false);
            }
        }

        _outboxProcessor?.TriggerImmediate();

        return new EmergencyReplayResult(
            pending.Count,
            successCount,
            duplicateCount,
            failedCount,
            errors.AsReadOnly());
    }
}
