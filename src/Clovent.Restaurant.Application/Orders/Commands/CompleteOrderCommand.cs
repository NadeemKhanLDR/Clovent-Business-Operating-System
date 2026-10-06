using Clovent.Inventory.Application.Transactions.Queries;
using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.Inventory.Transactions;
using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Sales;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Tables;
using System.Text.Json;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Outbox;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>
/// Completes an order: verifies its balance is zero, closes the order and vacates its table,
/// records an immutable receipt snapshot, and dispatches secondary work (inventory posting,
/// accounting, QuickBooks, receipts, analytics, recommendations, cloud sync) asynchronously
/// through the transactional outbox without stalling the cashier.
/// </summary>
public sealed record CompleteOrderCommand(Guid OrderId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="CompleteOrderCommand"/>.</summary>
public sealed class CompleteOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IDiscountRepository discountRepository,
    IServiceChargeRepository serviceChargeRepository,
    IPaymentRepository paymentRepository,
    ITableRepository tableRepository,
    IDailySalesSequenceRepository dailySalesSequenceRepository,
    IMediator mediator,
    IOutboxRepository? outboxRepository = null,
    IOutboxProcessor? outboxProcessor = null) : IRequestHandler<CompleteOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // Idempotency: if already completed, return existing completed order safely
        if (order.Status == OrderStatus.Completed)
        {
            return OrderDto.FromDomain(order);
        }

        var lines = await orderLineRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var discounts = await discountRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var serviceCharges = await serviceChargeRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var payments = await paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);

        var lineDtos = lines.Select(OrderLineDto.FromDomain).ToList();
        var totals = OrderTotalsCalculator.Calculate(
            lineDtos,
            discounts.Select(DiscountDto.FromDomain).ToList(),
            serviceCharges.Select(ServiceChargeDto.FromDomain).ToList(),
            payments.Select(PaymentDto.FromDomain).ToList());

        if (totals.Balance > 0.005m)
            throw RestaurantDomainException.OrderNotFullyPaid(orderId, totals.Balance);

        if (totals.Balance < -0.005m)
            throw RestaurantDomainException.OrderOverPaid(orderId, -totals.Balance);

        if (!order.DailySalesNumber.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
            var sequence = await dailySalesSequenceRepository.GetByWarehouseAndDateAsync(order.WarehouseId, today, cancellationToken);
            if (sequence is null)
            {
                sequence = DailySalesSequence.Create(order.WarehouseId, today);
                await dailySalesSequenceRepository.AddAsync(sequence, cancellationToken);
            }
            var nextDailyNumber = sequence.Next();
            order.AssignDailySalesNumber(nextDailyNumber);
        }

        // Freeze receipt snapshot immutably on the order
        var snapshotItems = lineDtos.Where(l => !l.IsVoided).Select(l => new ReceiptSnapshotItem(
            l.ProductVariantId,
            "",
            "",
            l.Quantity,
            l.UnitPrice,
            l.LineTotal,
            l.Notes)).ToList();

        var snapshotPayments = payments.Where(p => !p.IsVoided).Select(p => new ReceiptSnapshotPayment(
            p.Amount,
            "Payment")).ToList();

        var snapshot = new ReceiptSnapshot(
            order.Id.Value,
            order.OrderNumber.Value,
            order.DailySalesNumber,
            order.OrderType.ToString(),
            DateTimeOffset.UtcNow,
            snapshotItems,
            totals.Subtotal,
            totals.TaxTotal,
            totals.DiscountTotal,
            totals.ServiceChargeTotal,
            totals.GrandTotal,
            totals.Balance,
            snapshotPayments,
            null,
            Environment.MachineName,
            order.CustomerNotes);

        order.SetReceiptSnapshot(JsonSerializer.Serialize(snapshot));

        if (outboxRepository is not null)
        {
            // Transactional Outbox: decouple secondary work into atomic outbox messages
            var activeItems = lineDtos.Where(l => !l.IsVoided)
                .Select(l => new InventoryPostingLineItem(l.ProductVariantId, "", "", l.Quantity))
                .ToList();

            var invPayload = new InventoryPostingPayload(
                order.Id.Value,
                order.OrderNumber.Value,
                order.WarehouseId.Value,
                activeItems);

            var invMessage = OutboxMessage.Create(
                OutboxMessageType.InventoryPosting,
                "Order",
                order.Id.Value.ToString(),
                order.Id.Value.ToString(),
                JsonSerializer.Serialize(invPayload),
                $"inv-{order.Id.Value}");

            var qbPayload = new QuickBooksSyncPayload(
                order.Id.Value,
                order.OrderNumber.Value,
                totals.GrandTotal,
                payments.FirstOrDefault(p => !p.IsVoided)?.PaymentMethodId.ToString() ?? "Cash",
                null,
                DateTimeOffset.UtcNow);

            var qbMessage = OutboxMessage.Create(
                OutboxMessageType.QuickBooksSync,
                "Order",
                order.Id.Value.ToString(),
                order.Id.Value.ToString(),
                JsonSerializer.Serialize(qbPayload),
                $"qb-{order.Id.Value}");

            var printPayload = new ReceiptPrintPayload(
                order.Id.Value,
                order.OrderNumber.Value,
                $"Receipt for Order {order.OrderNumber.Value} - Total {totals.GrandTotal:N2}");

            var printMessage = OutboxMessage.Create(
                OutboxMessageType.ReceiptPrint,
                "Order",
                order.Id.Value.ToString(),
                order.Id.Value.ToString(),
                JsonSerializer.Serialize(printPayload),
                $"print-{order.Id.Value}");

            var analyticsPayload = new AnalyticsEventPayload(
                "OrderCompleted",
                order.Id.Value.ToString(),
                $"Order {order.OrderNumber.Value} completed with total {totals.GrandTotal:N2}",
                DateTimeOffset.UtcNow);

            var analyticsMessage = OutboxMessage.Create(
                OutboxMessageType.AnalyticsEvent,
                "Order",
                order.Id.Value.ToString(),
                order.Id.Value.ToString(),
                JsonSerializer.Serialize(analyticsPayload),
                $"analytics-{order.Id.Value}");

            var cloudPayload = new CloudSyncPayload(
                order.Id.Value,
                order.OrderNumber.Value,
                totals.GrandTotal,
                Guid.Empty,
                Guid.Empty,
                DateTimeOffset.UtcNow);

            var cloudMessage = OutboxMessage.Create(
                OutboxMessageType.CloudSync,
                "Order",
                order.Id.Value.ToString(),
                order.Id.Value.ToString(),
                JsonSerializer.Serialize(cloudPayload),
                $"cloud-{order.Id.Value}");

            var recPayload = new RecommendationLearningPayload(
                order.Id.Value,
                lineDtos.Where(l => !l.IsVoided).Select(l => l.ProductVariantId).ToList(),
                DateTimeOffset.UtcNow);

            var recMessage = OutboxMessage.Create(
                OutboxMessageType.RecommendationLearning,
                "Order",
                order.Id.Value.ToString(),
                order.Id.Value.ToString(),
                JsonSerializer.Serialize(recPayload),
                $"rec-{order.Id.Value}");

            await outboxRepository.AddRangeAsync([invMessage, qbMessage, printMessage, analyticsMessage, cloudMessage, recMessage], cancellationToken);
        }
        else
        {
            // Fallback for minimal test environments without outbox repository
            await IssueStockForOrderAsync(order, lineDtos, cancellationToken);
        }

        order.Complete();

        if (order.TableId is { } tableId)
        {
            var activeOrders = await orderRepository.GetOpenOrHeldByTableIdAsync(tableId, cancellationToken);
            if (!activeOrders.Any(o => o.Id != order.Id))
            {
                var table = await tableRepository.GetByIdAsync(tableId, cancellationToken);
                table?.Vacate();
            }
        }

        outboxProcessor?.TriggerImmediate();

        return OrderDto.FromDomain(order);
    }

    /// <summary>
    /// The <see cref="InventoryTransaction.ReferenceType"/> every stock movement
    /// this handler issues is stamped with, so those movements can be found
    /// again by order id.
    /// </summary>
    private const string StockReferenceType = "Order";

    /// <summary>
    /// Issues warehouse stock for every active line, in a way that is safe to
    /// retry: it checks the whole order can be satisfied before it moves
    /// anything, and skips whatever a previous attempt already moved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Root cause this closes (H-2):</b> stock used to be issued one line at
    /// a time inside a bare <c>foreach</c>. Each <see cref="IssueStockCommand"/>
    /// is a nested MediatR request, so it runs the whole pipeline - including
    /// Inventory's own <c>UnitOfWorkBehavior</c> - and <b>commits to the
    /// Inventory database immediately</b>, before this handler has done
    /// anything to the order. Restaurant and Inventory are separate databases
    /// (<c>Clovent_Restaurant</c> / <c>Clovent_Inventory</c>) on separate
    /// <c>DbContext</c>s, and the solution opens no explicit transaction
    /// anywhere, so those per-line commits are permanent the instant they
    /// happen. A five-line order whose fourth line was short therefore left
    /// lines one to three issued for good while
    /// <see cref="Order.Complete"/> never ran and the order stayed
    /// <see cref="OrderStatus.Open"/> - and completing it again re-issued those
    /// same three lines, silently depleting stock twice.
    /// </para>
    /// <para>
    /// <b>Why this is not a transaction:</b> two databases cannot share an EF
    /// Core transaction, and promoting to a distributed one (MSDTC) is a
    /// deployment burden this app does not carry. Instead the two failure
    /// modes are addressed directly - validate before the first write so the
    /// common failure never starts, and make the writes idempotent so any
    /// failure that still occurs is recoverable simply by retrying.
    /// </para>
    /// <para>
    /// Quantities are aggregated <em>per variant</em> rather than per line,
    /// because the same variant legitimately appears on several lines (a line
    /// carrying notes is kept separate from one without - see
    /// <c>RestaurantPosForm.AddProductToCurrentOrder</c>). Checking each line
    /// against stock independently would let two lines of six units each pass
    /// against a balance of ten, and it would make "has this variant already
    /// been issued?" ambiguous. The stock delta is unchanged; only the ledger
    /// granularity differs (one Issue row per variant instead of per line).
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Stock is insufficient for one or more variants. Thrown before anything is issued.</exception>
    private async Task IssueStockForOrderAsync(Order order, IReadOnlyCollection<OrderLineDto> lineDtos, CancellationToken cancellationToken)
    {
        HashSet<Guid> serviceVariantIds = [];
        try
        {
            var allVariants = await mediator.Send(new Clovent.Catalog.Application.Variants.Queries.ListProductVariantsQuery(), cancellationToken);
            if (allVariants is not null)
            {
                serviceVariantIds = allVariants
                    .Where(v => string.Equals(v.ItemType, "Service", StringComparison.OrdinalIgnoreCase))
                    .Select(v => v.ProductVariantId)
                    .ToHashSet();
            }
        }
        catch (NotSupportedException)
        {
            // Unit tests with minimal test fakes
        }

        var required = lineDtos
            .Where(l => !l.IsVoided && !serviceVariantIds.Contains(l.ProductVariantId))
            .GroupBy(l => l.ProductVariantId)
            .Select(g => new { ProductVariantId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToList();

        if (required.Count == 0)
        {
            return;
        }

        // Whatever an earlier, failed attempt already issued for this order.
        // Re-issuing those would deplete stock a second time.
        var alreadyIssued = await mediator.Send(
            new ListInventoryTransactionsByReferenceQuery(StockReferenceType, order.Id.Value), cancellationToken);
        var alreadyIssuedVariantIds = alreadyIssued
            .Where(t => t.TransactionType == nameof(InventoryTransactionType.Issue))
            .Select(t => t.ProductVariantId)
            .ToHashSet();

        // Pass one: resolve and verify everything, writing nothing. A variant
        // with no stock record at this warehouse is not tracked and is skipped,
        // exactly as before.
        var pending = new List<(Guid WarehouseStockId, decimal Quantity)>();
        var shortfalls = new List<string>();

        foreach (var item in required)
        {
            if (alreadyIssuedVariantIds.Contains(item.ProductVariantId))
            {
                continue;
            }

            var stock = await mediator.Send(
                new GetWarehouseStockByWarehouseAndVariantQuery(order.WarehouseId.Value, item.ProductVariantId), cancellationToken);
            if (stock is null)
            {
                continue;
            }

            if (!stock.AllowNegativeStock && stock.QuantityOnHand < item.Quantity)
            {
                shortfalls.Add($"variant {item.ProductVariantId}: {item.Quantity:N2} needed, {stock.QuantityOnHand:N2} on hand");
                continue;
            }

            pending.Add((stock.WarehouseStockId, item.Quantity));
        }

        if (shortfalls.Count > 0)
        {
            throw new InvalidOperationException(
                $"This order cannot be completed because there is not enough stock at its location.\n\n" +
                string.Join("\n", shortfalls) +
                "\n\nNo stock has been taken. Adjust the stock or void the affected lines, then complete the order again.");
        }

        // Pass two: every movement is now known to succeed.
        foreach (var (warehouseStockId, quantity) in pending)
        {
            await mediator.Send(
                new IssueStockCommand(
                    warehouseStockId,
                    quantity,
                    $"Order {order.OrderNumber.Value}",
                    StockReferenceType,
                    order.Id.Value),
                cancellationToken);
        }
    }
}
