using System.Text.Json;
using Clovent.Inventory.Application.Transactions.Queries;
using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.Inventory.Transactions;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.Refunds;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>
/// Idempotent outbox handler that issues warehouse stock for completed orders
/// or restocks returned items for completed refunds without stalling the cashier UI thread.
/// Enforces consistent inventory policy across tracked, untracked, and insufficient-stock products,
/// preventing inventory inflation and double-deductions.
/// </summary>
public sealed class InventoryPostingOutboxHandler(
    IMediator mediator,
    ILogger<InventoryPostingOutboxHandler> logger,
    IOutboxRepository? outboxRepository = null,
    IRefundRepository? refundRepository = null,
    Clovent.Inventory.Application.IUnitOfWork? inventoryUnitOfWork = null) : IOutboxMessageHandler
{
    private const string OrderStockReferenceType = "Order";
    private const string RefundStockReferenceType = "Refund";

    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.InventoryPosting;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<InventoryPostingPayload>(message.Payload)
            ?? throw new InvalidOperationException($"Invalid payload for message {message.Id}.");

        if (payload.Items.Count == 0)
        {
            return;
        }

        if (payload.IsRestock)
        {
            await HandleRestockAsync(payload, cancellationToken);
            return;
        }

        await HandleSaleIssueAsync(payload, cancellationToken);
    }

    private async Task HandleRestockAsync(InventoryPostingPayload payload, CancellationToken cancellationToken)
    {
        var referenceType = string.IsNullOrWhiteSpace(payload.ReferenceType)
            ? RefundStockReferenceType
            : payload.ReferenceType;

        var refundId = payload.OrderId;
        var originalOrderId = payload.OriginalOrderId;

        if (!originalOrderId.HasValue && refundRepository != null)
        {
            var refundEntity = await refundRepository.GetByIdAsync(new RefundId(refundId), cancellationToken);
            if (refundEntity != null)
            {
                originalOrderId = refundEntity.OrderId.Value;
            }
        }

        // Idempotency check: see what has already been restocked for this specific refund at this warehouse
        var alreadyRestocked = await mediator.Send(
            new ListInventoryTransactionsByReferenceQuery(referenceType, refundId), cancellationToken);

        var alreadyRestockedVariantIds = (alreadyRestocked ?? [])
            .Where(t => t.WarehouseId == payload.WarehouseId &&
                        t.TransactionType is nameof(InventoryTransactionType.Receipt) or nameof(InventoryTransactionType.OpeningBalance))
            .Select(t => t.ProductVariantId)
            .ToHashSet();

        var pendingRestockItems = payload.Items
            .GroupBy(i => i.ProductVariantId)
            .Select(g => new PendingRestockItem(g.Key, g.Sum(i => i.Quantity)))
            .Where(i => !alreadyRestockedVariantIds.Contains(i.ProductVariantId))
            .ToList();

        if (pendingRestockItems.Count == 0)
        {
            logger.LogInformation("All inventory restock items for {ReferenceType} {DocumentNumber} already received at warehouse {WarehouseId}. Skipping.",
                referenceType, payload.OrderNumber, payload.WarehouseId);
            return;
        }

        var quarantinedReasons = new List<string>();

        foreach (var item in pendingRestockItems)
        {
            // 1. Untracked check: If WarehouseStock does not exist, variant is untracked at this location.
            // Untracked items must NOT create phantom WarehouseStock or OpeningBalance on refund.
            var stock = await mediator.Send(
                new GetWarehouseStockByWarehouseAndVariantQuery(payload.WarehouseId, item.ProductVariantId), cancellationToken);

            if (stock is null)
            {
                logger.LogInformation(
                    "Product variant {VariantId} is not tracked at warehouse {WarehouseId}. Skipping refund restock movement.",
                    item.ProductVariantId, payload.WarehouseId);
                continue;
            }

            // 2. Correlation with original sale: Standalone restock fallback is disabled to prevent inventory fabrication.
            if (!originalOrderId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Inventory restock action quarantined for refund {payload.OrderNumber}: missing required original sale correlation. Standalone restock is disabled to prevent inventory fabrication.");
            }

            // 3. Execute entitlement check and stock receipt atomically under exclusive warehouse/variant lock
            if (inventoryUnitOfWork != null)
            {
                var itemQuarantineReason = await inventoryUnitOfWork.ExecuteStockMovementAtomicallyAsync(
                    payload.WarehouseId,
                    item.ProductVariantId,
                    async ct =>
                    {
                        return await ProcessSingleRestockItemAtomicallyAsync(payload, item, referenceType, refundId, originalOrderId.Value, ct);
                    },
                    async ct =>
                    {
                        logger.LogInformation("Duplicate reference detected during atomic restock for refund {RefundId}. Skipping.", refundId);
                        return (string?)null;
                    },
                    cancellationToken);

                if (!string.IsNullOrEmpty(itemQuarantineReason))
                {
                    quarantinedReasons.Add(itemQuarantineReason);
                }
            }
            else
            {
                var itemQuarantineReason = await ProcessSingleRestockItemAtomicallyAsync(payload, item, referenceType, refundId, originalOrderId.Value, cancellationToken);
                if (!string.IsNullOrEmpty(itemQuarantineReason))
                {
                    quarantinedReasons.Add(itemQuarantineReason);
                }
            }
        }

        // If all pending items were quarantined (0 could be restocked), throw quarantine exception to route message to DeadLetter / record diagnostic error
        if (quarantinedReasons.Count > 0 && quarantinedReasons.Count == pendingRestockItems.Count)
        {
            throw new InvalidOperationException(quarantinedReasons[0]);
        }

        logger.LogInformation("Successfully posted inventory restock for {ReferenceType} {DocumentNumber}.", referenceType, payload.OrderNumber);
    }

    private async Task<string?> ProcessSingleRestockItemAtomicallyAsync(
        InventoryPostingPayload payload,
        PendingRestockItem item,
        string referenceType,
        Guid refundId,
        Guid originalOrderId,
        CancellationToken cancellationToken)
    {
        // 1. Idempotency check inside the lock for this specific refund
        var existingRefundTxs = await mediator.Send(
            new ListInventoryTransactionsByReferenceQuery(referenceType, refundId), cancellationToken);

        var alreadyApplied = (existingRefundTxs ?? []).Any(t =>
            t.WarehouseId == payload.WarehouseId &&
            t.ProductVariantId == item.ProductVariantId &&
            t.TransactionType is nameof(InventoryTransactionType.Receipt) or nameof(InventoryTransactionType.OpeningBalance));

        if (alreadyApplied)
        {
            logger.LogInformation("Variant {VariantId} already restocked for {ReferenceType} {RefundId}. Skipping.",
                item.ProductVariantId, referenceType, refundId);
            return null;
        }

        // 2. Applicable sale depletion evidence: verify units issued at this warehouse for the original order
        var orderTransactions = await mediator.Send(
            new ListInventoryTransactionsByReferenceQuery(OrderStockReferenceType, originalOrderId), cancellationToken);

        var totalIssuedForOrder = (orderTransactions ?? [])
            .Where(t => t.WarehouseId == payload.WarehouseId &&
                        t.ProductVariantId == item.ProductVariantId &&
                        t.TransactionType == nameof(InventoryTransactionType.Issue))
            .Sum(t => t.Quantity);

        if (totalIssuedForOrder <= 0)
        {
            // Check if the original sale outbox message is still pending/retrying
            if (outboxRepository != null)
            {
                var saleMessage = await outboxRepository.GetByIdempotencyKeyAsync($"inv-{originalOrderId}", cancellationToken);
                if (saleMessage != null && saleMessage.Status is OutboxMessageStatus.Pending or OutboxMessageStatus.Processing or OutboxMessageStatus.RetryScheduled)
                {
                    // Original sale posting is still pending/retrying: defer restock until sale completes
                    throw new InvalidOperationException(
                        $"Original sale inventory posting for order {originalOrderId} is currently {saleMessage.Status}. Deferring restock retry.");
                }
            }

            // Quarantine the inventory action with explicit diagnostic reason - never silently invent stock.
            var reason = $"Inventory restock action quarantined for refund {payload.OrderNumber}: product variant {item.ProductVariantId} has no sale depletion evidence at warehouse {payload.WarehouseId} on order {originalOrderId}. Restock quarantined to prevent inventory fabrication.";
            logger.LogWarning("{QuarantineReason}", reason);
            return reason;
        }

        // 3. Calculate how much has already been restocked across other refunds for this order at this warehouse
        decimal alreadyRestockedForOrder = 0m;
        if (refundRepository != null)
        {
            var allOrderRefunds = await refundRepository.GetByOrderIdAsync(new OrderId(originalOrderId), cancellationToken);
            foreach (var otherRefund in allOrderRefunds.Where(r => r.Id.Value != refundId))
            {
                var otherRefundTxs = await mediator.Send(
                    new ListInventoryTransactionsByReferenceQuery(RefundStockReferenceType, otherRefund.Id.Value), cancellationToken);
                alreadyRestockedForOrder += (otherRefundTxs ?? [])
                    .Where(t => t.WarehouseId == payload.WarehouseId &&
                                t.ProductVariantId == item.ProductVariantId &&
                                t.TransactionType is nameof(InventoryTransactionType.Receipt) or nameof(InventoryTransactionType.OpeningBalance))
                    .Sum(t => t.Quantity);
            }
        }

        var remainingRestockable = Math.Max(0m, totalIssuedForOrder - alreadyRestockedForOrder);
        var effectiveRestockQty = Math.Min(item.Quantity, remainingRestockable);

        if (effectiveRestockQty <= 0)
        {
            var reason = $"Inventory restock action quarantined for refund {payload.OrderNumber}: all {totalIssuedForOrder} issued units for variant {item.ProductVariantId} on order {originalOrderId} at warehouse {payload.WarehouseId} have already been restocked. Restock quarantined to prevent inventory inflation.";
            logger.LogWarning("{QuarantineReason}", reason);
            return reason;
        }

        if (effectiveRestockQty < item.Quantity)
        {
            logger.LogWarning(
                "Restock requested {RequestedQty} for variant {VariantId} on {RefundNumber}, but only {RemainingRestockable} units remain restockable (Total issued: {TotalIssued}, Already restocked: {AlreadyRestocked}). Capping restock at {EffectiveQty}.",
                item.Quantity, item.ProductVariantId, payload.OrderNumber, remainingRestockable, totalIssuedForOrder, alreadyRestockedForOrder, effectiveRestockQty);
        }

        await mediator.Send(
            new OpenOrReceiveStockCommand(
                payload.WarehouseId,
                item.ProductVariantId,
                effectiveRestockQty,
                $"Restock from {referenceType} {payload.OrderNumber} (Order {payload.OriginalOrderNumber ?? originalOrderId.ToString()})",
                referenceType,
                refundId),
            cancellationToken);

        return null;
    }

    private async Task HandleSaleIssueAsync(InventoryPostingPayload payload, CancellationToken cancellationToken)
    {
        // Idempotency check: see what has already been issued for this order at this warehouse
        var alreadyIssued = await mediator.Send(
            new ListInventoryTransactionsByReferenceQuery(OrderStockReferenceType, payload.OrderId), cancellationToken);

        var alreadyIssuedVariantIds = (alreadyIssued ?? [])
            .Where(t => t.WarehouseId == payload.WarehouseId &&
                        t.TransactionType == nameof(InventoryTransactionType.Issue))
            .Select(t => t.ProductVariantId)
            .ToHashSet();

        var pendingItems = payload.Items
            .GroupBy(i => i.ProductVariantId)
            .Select(g => new PendingRestockItem(g.Key, g.Sum(i => i.Quantity)))
            .Where(i => !alreadyIssuedVariantIds.Contains(i.ProductVariantId))
            .ToList();

        if (pendingItems.Count == 0)
        {
            logger.LogInformation("All inventory items for order {OrderNumber} already issued at warehouse {WarehouseId}. Skipping.",
                payload.OrderNumber, payload.WarehouseId);
            return;
        }

        foreach (var item in pendingItems)
        {
            var stock = await mediator.Send(
                new GetWarehouseStockByWarehouseAndVariantQuery(payload.WarehouseId, item.ProductVariantId), cancellationToken);

            if (stock is null)
            {
                logger.LogInformation("Warehouse stock not found for variant {VariantId} at warehouse {WarehouseId}. Untracked item, skipping movement.", item.ProductVariantId, payload.WarehouseId);
                continue;
            }

            // IssueStockCommand will enforce AllowNegativeStock inside WarehouseStock aggregate:
            // if !AllowNegativeStock and stock.QuantityOnHand < quantity, it throws InsufficientStock
            // causing outbox message to retry with backoff.
            await mediator.Send(
                new IssueStockCommand(
                    stock.WarehouseStockId,
                    item.Quantity,
                    $"Order {payload.OrderNumber}",
                    OrderStockReferenceType,
                    payload.OrderId),
                cancellationToken);
        }

        logger.LogInformation("Successfully posted inventory for order {OrderNumber}.", payload.OrderNumber);
    }

    private sealed record PendingRestockItem(Guid ProductVariantId, decimal Quantity);
}
