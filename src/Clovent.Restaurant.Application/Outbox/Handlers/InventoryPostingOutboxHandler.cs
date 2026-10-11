using System.Text.Json;
using Clovent.Inventory.Application.Transactions.Queries;
using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.Inventory.Transactions;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Outbox;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>
/// Idempotent outbox handler that issues warehouse stock for completed orders
/// or restocks returned items for completed refunds without stalling the cashier UI thread.
/// </summary>
public sealed class InventoryPostingOutboxHandler(
    IMediator mediator,
    ILogger<InventoryPostingOutboxHandler> logger) : IOutboxMessageHandler
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
            var referenceType = string.IsNullOrWhiteSpace(payload.ReferenceType)
                ? RefundStockReferenceType
                : payload.ReferenceType;

            var alreadyRestocked = await mediator.Send(
                new ListInventoryTransactionsByReferenceQuery(referenceType, payload.OrderId), cancellationToken);

            var alreadyRestockedVariantIds = alreadyRestocked
                .Where(t => t.TransactionType is nameof(InventoryTransactionType.Receipt) or nameof(InventoryTransactionType.OpeningBalance))
                .Select(t => t.ProductVariantId)
                .ToHashSet();

            var pendingRestockItems = payload.Items
                .GroupBy(i => i.ProductVariantId)
                .Select(g => new { ProductVariantId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .Where(i => !alreadyRestockedVariantIds.Contains(i.ProductVariantId))
                .ToList();

            if (pendingRestockItems.Count == 0)
            {
                logger.LogInformation("All inventory restock items for {ReferenceType} {DocumentNumber} already received. Skipping.", referenceType, payload.OrderNumber);
                return;
            }

            foreach (var item in pendingRestockItems)
            {
                await mediator.Send(
                    new OpenOrReceiveStockCommand(
                        payload.WarehouseId,
                        item.ProductVariantId,
                        item.Quantity,
                        $"Restock from {referenceType} {payload.OrderNumber}",
                        referenceType,
                        payload.OrderId),
                    cancellationToken);
            }

            logger.LogInformation("Successfully posted inventory restock for {ReferenceType} {DocumentNumber}.", referenceType, payload.OrderNumber);
            return;
        }

        // Idempotency check: see what has already been issued for this order
        var alreadyIssued = await mediator.Send(
            new ListInventoryTransactionsByReferenceQuery(OrderStockReferenceType, payload.OrderId), cancellationToken);

        var alreadyIssuedVariantIds = alreadyIssued
            .Where(t => t.TransactionType == nameof(InventoryTransactionType.Issue))
            .Select(t => t.ProductVariantId)
            .ToHashSet();

        var pendingItems = payload.Items
            .GroupBy(i => i.ProductVariantId)
            .Select(g => new { ProductVariantId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .Where(i => !alreadyIssuedVariantIds.Contains(i.ProductVariantId))
            .ToList();

        if (pendingItems.Count == 0)
        {
            logger.LogInformation("All inventory items for order {OrderNumber} already issued. Skipping.", payload.OrderNumber);
            return;
        }

        foreach (var item in pendingItems)
        {
            var stock = await mediator.Send(
                new GetWarehouseStockByWarehouseAndVariantQuery(payload.WarehouseId, item.ProductVariantId), cancellationToken);

            if (stock is null)
            {
                logger.LogWarning("Warehouse stock not found for variant {VariantId} at warehouse {WarehouseId}. Skipping movement.", item.ProductVariantId, payload.WarehouseId);
                continue;
            }

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
}
