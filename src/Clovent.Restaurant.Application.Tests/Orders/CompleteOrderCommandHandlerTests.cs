using Clovent.Inventory.Application.Transactions.Dtos;
using Clovent.Inventory.Application.Transactions.Queries;
using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.Inventory.Transactions;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Tables;
using MediatR;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Orders;

public class CompleteOrderCommandHandlerTests
{
    /// <summary>
    /// Stands in for the Inventory bounded context, reproducing the one property
    /// that makes H-2 possible: an <see cref="IssueStockCommand"/> is a nested
    /// MediatR request, so it runs Inventory's own <c>UnitOfWorkBehavior</c> and
    /// <b>commits immediately</b> - its effect survives the outer
    /// <c>CompleteOrderCommand</c> failing afterwards. Both the balance and the
    /// ledger here therefore persist across a failed attempt, exactly as the two
    /// real databases do, which is what lets the retry test be meaningful.
    /// </summary>
    private sealed class FakeInventory
    {
        private sealed record StockRow(Guid WarehouseStockId, Guid ProductVariantId, bool AllowNegativeStock)
        {
            public decimal QuantityOnHand { get; set; }
        }

        private readonly Dictionary<Guid, StockRow> _stockByVariant = [];
        private readonly List<InventoryTransactionDto> _ledger = [];

        /// <summary>Every <see cref="IssueStockCommand"/> that actually reached the Inventory context.</summary>
        public List<(Guid WarehouseStockId, decimal Quantity)> IssuedCalls { get; } = [];

        /// <summary>Registers a stock balance for <paramref name="productVariantId"/> and returns its warehouse-stock id.</summary>
        public Guid AddStock(Guid productVariantId, decimal quantityOnHand, bool allowNegativeStock = false)
        {
            var row = new StockRow(Guid.NewGuid(), productVariantId, allowNegativeStock) { QuantityOnHand = quantityOnHand };
            _stockByVariant[productVariantId] = row;
            return row.WarehouseStockId;
        }

        /// <summary>The balance currently on hand - the assertion that proves stock was, or was not, taken.</summary>
        public decimal OnHand(Guid productVariantId) => _stockByVariant[productVariantId].QuantityOnHand;

        public IMediator CreateMediator() => new FakeMediator(request =>
        {
            object? response = request switch
            {
                GetWarehouseStockByWarehouseAndVariantQuery q => ResolveStock(q),
                ListInventoryTransactionsByReferenceQuery q => ResolveLedger(q),
                IssueStockCommand c => Issue(c),
                _ => throw new NotSupportedException(request.GetType().Name),
            };
            return Task.FromResult(response);
        });

        private object? ResolveStock(GetWarehouseStockByWarehouseAndVariantQuery query) =>
            _stockByVariant.TryGetValue(query.ProductVariantId, out var row)
                ? new WarehouseStockDto(
                    row.WarehouseStockId, query.WarehouseId, row.ProductVariantId,
                    row.QuantityOnHand, 0, row.QuantityOnHand, 0, 0, row.AllowNegativeStock,
                    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
                : null;

        private IReadOnlyCollection<InventoryTransactionDto> ResolveLedger(ListInventoryTransactionsByReferenceQuery query) =>
            [.. _ledger.Where(t => t.ReferenceType == query.ReferenceType && t.ReferenceId == query.ReferenceId)];

        private WarehouseStockDto Issue(IssueStockCommand command)
        {
            var row = _stockByVariant.Values.First(r => r.WarehouseStockId == command.WarehouseStockId);

            // Mirrors WarehouseStock.Issue: refuses to go negative.
            if (!row.AllowNegativeStock && row.QuantityOnHand - command.Quantity < 0)
                throw new InvalidOperationException($"Insufficient stock for {row.ProductVariantId}.");

            row.QuantityOnHand -= command.Quantity;
            IssuedCalls.Add((command.WarehouseStockId, command.Quantity));

            // Committed immediately, and therefore still present on a retry.
            _ledger.Add(new InventoryTransactionDto(
                Guid.NewGuid(), Guid.NewGuid(), row.ProductVariantId,
                nameof(InventoryTransactionType.Issue), command.Quantity,
                command.ReferenceType, command.ReferenceId, command.Notes, DateTimeOffset.UtcNow));

            return new WarehouseStockDto(
                row.WarehouseStockId, Guid.NewGuid(), row.ProductVariantId,
                row.QuantityOnHand, 0, row.QuantityOnHand, 0, 0, row.AllowNegativeStock,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        }
    }

    /// <summary>Every repository a <see cref="CompleteOrderCommandHandler"/> needs, kept together so each test states only what it is actually about.</summary>
    private sealed class Fixture
    {
        public FakeOrderRepository Orders { get; } = new();
        public FakeOrderLineRepository OrderLines { get; } = new();
        public FakeDiscountRepository Discounts { get; } = new();
        public FakeServiceChargeRepository ServiceCharges { get; } = new();
        public FakePaymentRepository Payments { get; } = new();
        public FakeTableRepository Tables { get; } = new();
        public FakeDailySalesSequenceRepository DailySalesSequences { get; } = new();
        public FakeInventory Inventory { get; } = new();

        public CompleteOrderCommandHandler CreateHandler() => new(
            Orders, OrderLines, Discounts, ServiceCharges, Payments, Tables, DailySalesSequences, Inventory.CreateMediator());

        /// <summary>Adds a line to <paramref name="order"/> and registers it, returning the variant it sells.</summary>
        public Guid AddLine(Order order, decimal quantity, decimal unitPrice)
        {
            var variantId = Clovent.Catalog.Variants.ProductVariantId.New();
            var line = OrderLine.Create(order.Id, variantId, quantity, unitPrice, 0, false);
            order.AddOrderLine(line.Id);
            OrderLines.Add(line);
            return variantId.Value;
        }

        /// <summary>Settles <paramref name="order"/> in full, so completion is never blocked by an outstanding balance.</summary>
        public void PayInFull(Order order, decimal amount)
        {
            var payment = Payment.Create(order.Id, PaymentMethodId.New(), amount);
            order.RecordPayment(payment.Id);
            Payments.Add(payment);
        }
    }

    [Fact]
    public async Task Handle_FullyPaid_CompletesAndIssuesStock()
    {
        var fixture = new Fixture();

        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        table.Occupy();
        fixture.Tables.Add(table);

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        fixture.Orders.Add(order);
        var variantId = fixture.AddLine(order, quantity: 2, unitPrice: 10m);
        var warehouseStockId = fixture.Inventory.AddStock(variantId, quantityOnHand: 100);
        fixture.PayInFull(order, 20m);

        var result = await fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.Equal("Available", table.OccupancyStatus.ToString());
        Assert.Single(fixture.Inventory.IssuedCalls);
        Assert.Equal((warehouseStockId, 2m), fixture.Inventory.IssuedCalls[0]);
        Assert.Equal(98m, fixture.Inventory.OnHand(variantId));
        Assert.Equal(1, result.DailySalesNumber);
    }

    [Fact]
    public async Task Handle_NotFullyPaid_Throws()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var variantId = fixture.AddLine(order, quantity: 1, unitPrice: 50m);
        fixture.Inventory.AddStock(variantId, quantityOnHand: 100);

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None));

        Assert.Empty(fixture.Inventory.IssuedCalls);
    }

    /// <summary>M-4 scenario 1: paid to the penny - completion succeeds.</summary>
    [Fact]
    public async Task Handle_ExactPayment_Completes()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var variantId = fixture.AddLine(order, quantity: 2, unitPrice: 25m);
        fixture.Inventory.AddStock(variantId, quantityOnHand: 10);
        fixture.PayInFull(order, 50m);

        var result = await fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
    }

    /// <summary>M-4 scenario 2: still owing - completion rejected, and no stock moves.</summary>
    [Fact]
    public async Task Handle_PartialPayment_Rejected()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var variantId = fixture.AddLine(order, quantity: 2, unitPrice: 25m);
        fixture.Inventory.AddStock(variantId, quantityOnHand: 10);
        fixture.PayInFull(order, 30m);

        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None));

        Assert.Contains("still owes", ex.Message);
        Assert.Equal("Open", order.Status.ToString());
        Assert.Empty(fixture.Inventory.IssuedCalls);
    }

    /// <summary>
    /// M-4 scenario 3 - the ORD-35 case. 660.00 taken against a 280.00 bill
    /// used to complete silently; it must now be refused.
    /// </summary>
    [Fact]
    public async Task Handle_OverpaidOrder_Rejected()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var variantId = fixture.AddLine(order, quantity: 1, unitPrice: 280m);
        fixture.Inventory.AddStock(variantId, quantityOnHand: 10);

        // Four payments of 165.00, exactly as ORD-35 carries.
        foreach (var _ in Enumerable.Range(0, 4))
        {
            fixture.PayInFull(order, 165m);
        }

        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None));

        Assert.Contains("over-paid by 380.00", ex.Message);
        Assert.Equal("Open", order.Status.ToString());
        Assert.Empty(fixture.Inventory.IssuedCalls);
    }

    /// <summary>M-4 scenario 4: a zero-value bill with no payment balances at zero and still completes.</summary>
    [Fact]
    public async Task Handle_ZeroBalanceOrder_Completes()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var variantId = fixture.AddLine(order, quantity: 1, unitPrice: 0m);
        fixture.Inventory.AddStock(variantId, quantityOnHand: 10);

        var result = await fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
    }

    /// <summary>M-4: an over-payment within the half-cent tolerance is rounding, not an over-payment, and must still complete.</summary>
    [Fact]
    public async Task Handle_OverpaidWithinTolerance_Completes()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var variantId = fixture.AddLine(order, quantity: 1, unitPrice: 50m);
        fixture.Inventory.AddStock(variantId, quantityOnHand: 10);
        fixture.PayInFull(order, 50.004m);

        var result = await fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
    }

    /// <summary>H-2 scenario 1: everything available - completion succeeds and every line is issued.</summary>
    [Fact]
    public async Task Handle_AllStockAvailable_CompletesAndIssuesEveryLine()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var first = fixture.AddLine(order, quantity: 2, unitPrice: 10m);
        var second = fixture.AddLine(order, quantity: 3, unitPrice: 10m);
        var third = fixture.AddLine(order, quantity: 1, unitPrice: 10m);
        fixture.Inventory.AddStock(first, quantityOnHand: 10);
        fixture.Inventory.AddStock(second, quantityOnHand: 10);
        fixture.Inventory.AddStock(third, quantityOnHand: 10);
        fixture.PayInFull(order, 60m);

        var result = await fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.Equal(3, fixture.Inventory.IssuedCalls.Count);
        Assert.Equal(8m, fixture.Inventory.OnHand(first));
        Assert.Equal(7m, fixture.Inventory.OnHand(second));
        Assert.Equal(9m, fixture.Inventory.OnHand(third));
    }

    /// <summary>H-2 scenario 2: the very first line is short - nothing is issued at all.</summary>
    [Fact]
    public async Task Handle_InsufficientStockOnFirstLine_IssuesNothing()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var first = fixture.AddLine(order, quantity: 5, unitPrice: 10m);
        var second = fixture.AddLine(order, quantity: 2, unitPrice: 10m);
        fixture.Inventory.AddStock(first, quantityOnHand: 1);
        fixture.Inventory.AddStock(second, quantityOnHand: 10);
        fixture.PayInFull(order, 70m);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None));

        Assert.Contains("not enough stock", ex.Message);
        Assert.Empty(fixture.Inventory.IssuedCalls);
        Assert.Equal(1m, fixture.Inventory.OnHand(first));
        Assert.Equal(10m, fixture.Inventory.OnHand(second));
        Assert.Equal("Open", order.Status.ToString());
    }

    /// <summary>
    /// H-2 scenario 3 - the regression this fix exists for. The middle line is
    /// short; the lines before it must not be left permanently issued.
    /// </summary>
    [Fact]
    public async Task Handle_InsufficientStockOnMiddleLine_DoesNotIssueEarlierLines()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var first = fixture.AddLine(order, quantity: 2, unitPrice: 10m);
        var second = fixture.AddLine(order, quantity: 2, unitPrice: 10m);
        var short_ = fixture.AddLine(order, quantity: 9, unitPrice: 10m);
        var last = fixture.AddLine(order, quantity: 1, unitPrice: 10m);
        fixture.Inventory.AddStock(first, quantityOnHand: 10);
        fixture.Inventory.AddStock(second, quantityOnHand: 10);
        fixture.Inventory.AddStock(short_, quantityOnHand: 3);
        fixture.Inventory.AddStock(last, quantityOnHand: 10);
        fixture.PayInFull(order, 140m);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None));

        Assert.Contains("not enough stock", ex.Message);
        Assert.Empty(fixture.Inventory.IssuedCalls);
        Assert.Equal(10m, fixture.Inventory.OnHand(first));
        Assert.Equal(10m, fixture.Inventory.OnHand(second));
        Assert.Equal(3m, fixture.Inventory.OnHand(short_));
        Assert.Equal(10m, fixture.Inventory.OnHand(last));
        Assert.Equal("Open", order.Status.ToString());
    }

    /// <summary>
    /// H-2 scenario 4: an attempt that issued stock and then failed before the
    /// order was saved must not deplete that stock a second time when retried.
    /// The first attempt is forced to fail after the first variant is issued by
    /// making a later variant short; restocking it then lets the retry through.
    /// </summary>
    [Fact]
    public async Task Handle_RetryAfterFailedCompletion_DoesNotIssueStockTwice()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var first = fixture.AddLine(order, quantity: 2, unitPrice: 10m);
        var second = fixture.AddLine(order, quantity: 4, unitPrice: 10m);
        var firstStockId = fixture.Inventory.AddStock(first, quantityOnHand: 10);
        fixture.Inventory.AddStock(second, quantityOnHand: 1);
        fixture.PayInFull(order, 60m);

        var handler = fixture.CreateHandler();

        // Attempt 1 fails: the second variant is short.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None));
        Assert.Empty(fixture.Inventory.IssuedCalls);

        // Simulate the pre-flight passing but the request failing partway, by
        // issuing the first variant directly against this order's reference -
        // exactly what a committed nested IssueStockCommand leaves behind.
        await fixture.Inventory.CreateMediator().Send(
            new IssueStockCommand(firstStockId, 2m, $"Order {order.OrderNumber.Value}", "Order", order.Id.Value),
            CancellationToken.None);
        Assert.Equal(8m, fixture.Inventory.OnHand(first));

        // Restock the short variant and retry.
        fixture.Inventory.AddStock(second, quantityOnHand: 10);
        var result = await handler.Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Completed", result.Status);

        // The already-issued variant must be untouched by the retry: still 8, not 6.
        Assert.Equal(8m, fixture.Inventory.OnHand(first));
        Assert.Equal(6m, fixture.Inventory.OnHand(second));
    }

    /// <summary>H-2 scenario 5: a successful completion issues each variant exactly once.</summary>
    [Fact]
    public async Task Handle_SuccessfulCompletion_IssuesStockExactlyOnce()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        var variantId = fixture.AddLine(order, quantity: 3, unitPrice: 10m);
        fixture.Inventory.AddStock(variantId, quantityOnHand: 10);
        fixture.PayInFull(order, 30m);

        await fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Single(fixture.Inventory.IssuedCalls);
        Assert.Equal(7m, fixture.Inventory.OnHand(variantId));
    }

    /// <summary>
    /// The same variant on two lines (one carries notes, one does not) is a
    /// single stock movement for the combined quantity - and must be checked
    /// against stock as a combined quantity, or two lines of 6 would pass
    /// against a balance of 10 and drive it negative.
    /// </summary>
    [Fact]
    public async Task Handle_SameVariantOnTwoLines_ChecksAndIssuesCombinedQuantity()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);

        var variantId = Clovent.Catalog.Variants.ProductVariantId.New();
        var plain = OrderLine.Create(order.Id, variantId, 6, 10m, 0, false);
        var withNotes = OrderLine.Create(order.Id, variantId, 6, 10m, 0, false, "extra spicy");
        order.AddOrderLine(plain.Id);
        order.AddOrderLine(withNotes.Id);
        fixture.OrderLines.Add(plain);
        fixture.OrderLines.Add(withNotes);
        fixture.Inventory.AddStock(variantId.Value, quantityOnHand: 10);
        fixture.PayInFull(order, 120m);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None));

        Assert.Contains("not enough stock", ex.Message);
        Assert.Empty(fixture.Inventory.IssuedCalls);
        Assert.Equal(10m, fixture.Inventory.OnHand(variantId.Value));
    }

    /// <summary>A variant with no stock record at the order's warehouse is not tracked, and is skipped rather than blocking completion - unchanged behavior.</summary>
    [Fact]
    public async Task Handle_VariantWithNoStockRecord_IsSkippedAndCompletionSucceeds()
    {
        var fixture = new Fixture();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        fixture.Orders.Add(order);
        fixture.AddLine(order, quantity: 2, unitPrice: 10m);
        fixture.PayInFull(order, 20m);

        var result = await fixture.CreateHandler().Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.Empty(fixture.Inventory.IssuedCalls);
    }
}
