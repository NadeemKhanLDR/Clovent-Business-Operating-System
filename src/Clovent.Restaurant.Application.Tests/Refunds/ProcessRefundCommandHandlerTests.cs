using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Refunds;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Refunds;

public class ProcessRefundCommandHandlerTests
{
    private readonly FakeRefundRepository _refundRepo = new();
    private readonly FakeOrderRepository _orderRepo = new();
    private readonly FakeOrderLineRepository _orderLineRepo = new();
    private readonly FakePaymentRepository _paymentRepo = new();
    private readonly FakeCustomerRepository _customerRepo = new();
    private readonly FakeCustomerLedgerEntryRepository _customerLedgerRepo = new();
    private readonly FakeActivityLogEntryRepository _activityLogRepo = new();
    private readonly FakeMediator _mediator = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private ProcessRefundCommandHandler CreateHandler() =>
        new(
            _refundRepo,
            _orderRepo,
            _orderLineRepo,
            _paymentRepo,
            _customerRepo,
            _customerLedgerRepo,
            _activityLogRepo,
            _mediator,
            _unitOfWork);

    private async Task<(Order order, OrderLine line1, OrderLine line2, Payment payment)> CreateCompletedOrderWithLinesAsync()
    {
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        await _orderRepo.AddAsync(order);

        // Line 1: 2 units @ 500 = 1000 gross. Disc = 100. Base = 900. Tax 16% = 144. Total = 1044.
        var line1 = OrderLine.Create(order.Id, ProductVariantId.New(), 2m, 500m, 16m, false);
        line1.SetTaxSnapshot("Taxable", "PRA", "PK-PRA-16", 900m, 100m, 144m, "1.3.0-AwayFromZero-v1");
        order.AddOrderLine(line1.Id);
        await _orderLineRepo.AddAsync(line1);

        // Line 2: 1 unit @ 300 = 300 gross. Disc = 0. Base = 300. Tax 16% = 48. Total = 348.
        var line2 = OrderLine.Create(order.Id, ProductVariantId.New(), 1m, 300m, 16m, false);
        line2.SetTaxSnapshot("Taxable", "PRA", "PK-PRA-16", 300m, 0m, 48m, "1.3.0-AwayFromZero-v1");
        order.AddOrderLine(line2.Id);
        await _orderLineRepo.AddAsync(line2);

        // Total order payable = 1044 + 348 = 1392
        var payment = Payment.Create(order.Id, PaymentMethodId.New(), 1392m, null);
        order.RecordPayment(payment.Id);
        await _paymentRepo.AddAsync(payment);

        order.AssignDailySalesNumber(1);
        order.Complete();
        return (order, line1, line2, payment);
    }

    [Fact]
    public async Task ProcessRefund_FullRefund_ReversesAllLinesDiscountsAndTaxes()
    {
        var (order, line1, line2, _) = await CreateCompletedOrderWithLinesAsync();
        var handler = CreateHandler();

        var command = new ProcessRefundCommand(
            order.Id.Value,
            [
                new RefundItemRequest(line1.Id.Value, 2m, InventoryDisposition.Restock),
                new RefundItemRequest(line2.Id.Value, 1m, InventoryDisposition.Restock)
            ],
            "Customer cancelled order after handover",
            RefundSettlementMethod.CashPayout,
            "REQ-FULL-001",
            Guid.NewGuid(),
            "Cashier Alice");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1300.00m, result.SubtotalRefunded); // 1000 + 300
        Assert.Equal(100.00m, result.DiscountReversedTotal);
        Assert.Equal(192.00m, result.TaxReversedTotal);   // 144 + 48
        Assert.Equal(1392.00m, result.GrandTotalRefunded); // 1044 + 348
        Assert.Equal(2, result.Lines.Count);

        var savedRefund = await _refundRepo.GetByIdempotencyKeyAsync("REQ-FULL-001");
        Assert.NotNull(savedRefund);
    }

    [Fact]
    public async Task ProcessRefund_PartialRefund_ProportionallyReversesAmounts()
    {
        var (order, line1, _, _) = await CreateCompletedOrderWithLinesAsync();
        var handler = CreateHandler();

        // Line 1 has 2 units (1000 gross, 100 disc, 144 tax). Refund 1 unit (50%).
        var command = new ProcessRefundCommand(
            order.Id.Value,
            [
                new RefundItemRequest(line1.Id.Value, 1m, InventoryDisposition.Restock)
            ],
            "Partial return 1 of 2 burgers",
            RefundSettlementMethod.CashPayout,
            "REQ-PARTIAL-001",
            Guid.NewGuid(),
            "Cashier Alice");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(500.00m, result.SubtotalRefunded);
        Assert.Equal(50.00m, result.DiscountReversedTotal);
        Assert.Equal(72.00m, result.TaxReversedTotal);
        Assert.Equal(522.00m, result.GrandTotalRefunded);
    }

    [Fact]
    public async Task ProcessRefund_RepeatedPartialRefund_FinalStepConsumesExactResidual()
    {
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        await _orderRepo.AddAsync(order);

        // 3 items @ 33.33 = 99.99 gross. Allocated discount = 10.00. Exclusive tax 16% = 14.40. Net = 104.39.
        var line = OrderLine.Create(order.Id, ProductVariantId.New(), 3m, 33.33m, 16m, false);
        line.SetTaxSnapshot("Taxable", "PRA", "PK-PRA-16", 89.99m, 10.00m, 14.40m, "1.3.0-AwayFromZero-v1");
        order.AddOrderLine(line.Id);
        await _orderLineRepo.AddAsync(line);

        var payment = Payment.Create(order.Id, PaymentMethodId.New(), 104.39m, null);
        order.RecordPayment(payment.Id);
        await _paymentRepo.AddAsync(payment);
        order.AssignDailySalesNumber(1);
        order.Complete();

        var handler = CreateHandler();

        // 1st partial refund: 1 unit
        var cmd1 = new ProcessRefundCommand(
            order.Id.Value,
            [new RefundItemRequest(line.Id.Value, 1m)],
            "1st return",
            RefundSettlementMethod.CashPayout,
            "REQ-STEP-1",
            Guid.NewGuid(),
            "Cashier");
        var res1 = await handler.Handle(cmd1, CancellationToken.None);

        // 2nd partial refund: 1 unit
        var cmd2 = new ProcessRefundCommand(
            order.Id.Value,
            [new RefundItemRequest(line.Id.Value, 1m)],
            "2nd return",
            RefundSettlementMethod.CashPayout,
            "REQ-STEP-2",
            Guid.NewGuid(),
            "Cashier");
        var res2 = await handler.Handle(cmd2, CancellationToken.None);

        // 3rd and final partial refund: 1 unit (consumes exact remaining residual)
        var cmd3 = new ProcessRefundCommand(
            order.Id.Value,
            [new RefundItemRequest(line.Id.Value, 1m)],
            "3rd return",
            RefundSettlementMethod.CashPayout,
            "REQ-STEP-3",
            Guid.NewGuid(),
            "Cashier");
        var res3 = await handler.Handle(cmd3, CancellationToken.None);

        decimal totalGrossRefunded = res1.SubtotalRefunded + res2.SubtotalRefunded + res3.SubtotalRefunded;
        decimal totalDiscReversed = res1.DiscountReversedTotal + res2.DiscountReversedTotal + res3.DiscountReversedTotal;
        decimal totalTaxReversed = res1.TaxReversedTotal + res2.TaxReversedTotal + res3.TaxReversedTotal;
        decimal totalNetRefunded = res1.GrandTotalRefunded + res2.GrandTotalRefunded + res3.GrandTotalRefunded;

        Assert.Equal(99.99m, totalGrossRefunded);
        Assert.Equal(10.00m, totalDiscReversed);
        Assert.Equal(14.40m, totalTaxReversed);
        Assert.Equal(104.39m, totalNetRefunded);
    }

    [Fact]
    public async Task ProcessRefund_QuantityExceedingRemaining_ThrowsRefundExceedsRemainingEligibility()
    {
        var (order, line1, _, _) = await CreateCompletedOrderWithLinesAsync();
        var handler = CreateHandler();

        // First refund takes 1 unit of line1 (leaving 1 unit remaining)
        await handler.Handle(new ProcessRefundCommand(
            order.Id.Value,
            [new RefundItemRequest(line1.Id.Value, 1m)],
            "First",
            RefundSettlementMethod.CashPayout,
            "IDEM-EXCEED-1",
            Guid.NewGuid(),
            "Cashier"), CancellationToken.None);

        // Second refund attempts to take 2 units (only 1 remaining)
        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new ProcessRefundCommand(
                order.Id.Value,
                [new RefundItemRequest(line1.Id.Value, 2m)],
                "Second",
                RefundSettlementMethod.CashPayout,
                "IDEM-EXCEED-2",
                Guid.NewGuid(),
                "Cashier"), CancellationToken.None));

        Assert.Contains("exceeds remaining refundable quantity", ex.Message);
    }

    [Fact]
    public async Task ProcessRefund_IdempotentRetransmission_ReturnsExistingRefundWithoutDoublePosting()
    {
        var (order, line1, _, _) = await CreateCompletedOrderWithLinesAsync();
        var handler = CreateHandler();

        var command = new ProcessRefundCommand(
            order.Id.Value,
            [new RefundItemRequest(line1.Id.Value, 1m)],
            "Reason",
            RefundSettlementMethod.CashPayout,
            "SAME-IDEMPOTENCY-KEY",
            Guid.NewGuid(),
            "Cashier");

        var firstResult = await handler.Handle(command, CancellationToken.None);
        var secondResult = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(firstResult.RefundId, secondResult.RefundId);
        Assert.Equal(firstResult.RefundNumber, secondResult.RefundNumber);
        Assert.Single(await _refundRepo.GetAllAsync());
    }

    [Fact]
    public async Task ProcessRefund_ConflictingIdempotencyKey_ThrowsIdempotencyKeyConflict()
    {
        var (order, line1, line2, _) = await CreateCompletedOrderWithLinesAsync();
        var handler = CreateHandler();

        var command1 = new ProcessRefundCommand(
            order.Id.Value,
            [new RefundItemRequest(line1.Id.Value, 1m)],
            "Reason",
            RefundSettlementMethod.CashPayout,
            "SHARED-KEY",
            Guid.NewGuid(),
            "Cashier");
        await handler.Handle(command1, CancellationToken.None);

        // Command 2 attempts to use same key for line 2
        var command2 = new ProcessRefundCommand(
            order.Id.Value,
            [new RefundItemRequest(line2.Id.Value, 1m)],
            "Different request",
            RefundSettlementMethod.CashPayout,
            "SHARED-KEY",
            Guid.NewGuid(),
            "Cashier");

        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(command2, CancellationToken.None));

        Assert.Contains("conflicting refund parameters", ex.Message);
    }
}
