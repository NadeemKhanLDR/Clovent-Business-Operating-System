using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.DayClose.Commands;
using Clovent.Restaurant.Application.DayClose.Queries;
using Clovent.Restaurant.Application.Shifts.Services;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.DayClose;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Refunds;
using Clovent.Restaurant.Shifts;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.DayClose;

public class BusinessDayCloseTaxRefundIntegrationTests
{
    private readonly FakeBusinessDayCloseRepository _dayCloseRepo = new();
    private readonly FakeShiftRepository _shiftRepo = new();
    private readonly FakePaymentRepository _paymentRepo = new();
    private readonly FakePaymentMethodRepository _paymentMethodRepo = new();
    private readonly FakeOrderRepository _orderRepo = new();
    private readonly FakeOrderLineRepository _orderLineRepo = new();
    private readonly FakeRefundRepository _refundRepo = new();
    private readonly FakeActivityLogEntryRepository _activityLogRepo = new();
    private readonly FakeMediator _mediator = new();
    private readonly BusinessDateProvider _dateProvider = new();

    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _terminalId = Guid.NewGuid();
    private readonly Guid _cashierId = Guid.NewGuid();
    private readonly DateOnly _businessDate = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task GetBusinessDaySummary_WithSalesAndRefunds_PopulatesTaxDiscountsAndRefunds()
    {
        // 1. Create a closed shift
        var shift = Shift.Open(
            1001,
            new BranchId(_branchId),
            new WarehouseId(_warehouseId),
            new TerminalId(_terminalId),
            new UserId(_cashierId),
            "Alice Cashier",
            500m);
        shift.Close(1500m, 1500m, null, "Balanced");
        await _shiftRepo.AddAsync(shift);

        // 2. Setup Cash Payment Method
        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        await _paymentMethodRepo.AddAsync(cashMethod);

        // 3. Create completed Order with tax and discount
        var order = Order.Create(OrderType.TakeAway, new WarehouseId(_warehouseId));
        await _orderRepo.AddAsync(order);

        var line = OrderLine.Create(order.Id, ProductVariantId.New(), 2m, 500m, 16m, false);
        line.SetTaxSnapshot("Taxable", "PRA", "PK-PRA-16", 900m, 100m, 144m, "1.3.0-AwayFromZero-v1");
        order.AddOrderLine(line.Id);
        await _orderLineRepo.AddAsync(line);

        var payment = Payment.Create(order.Id, cashMethod.Id, 1044m, shift.Id);
        order.RecordPayment(payment.Id);
        await _paymentRepo.AddAsync(payment);

        var receiptSnapshot = new ReceiptSnapshot(
            order.Id.Value, "ORD-1", 1, "TakeAway", DateTimeOffset.UtcNow,
            [], 1000m, 144m, 100m, 0m, 1044m, 0m, [], "Alice", "T1", null);
        order.SetReceiptSnapshot(JsonSerializer.Serialize(receiptSnapshot));
        order.AssignDailySalesNumber(1);
        order.Complete();

        // 4. Create posted Refund for this branch
        var refund = Refund.Create(
            RefundNumber.Generate(DateTimeOffset.UtcNow),
            order.Id,
            new BranchId(_branchId),
            new WarehouseId(_warehouseId),
            _cashierId,
            "Alice Cashier",
            "Return",
            "IDEM-DAYCLOSE-1",
            RefundSettlementMethod.CashPayout);
        refund.FinalizeFinancials(500m, 50m, 72m, 522m, "{}");
        await _refundRepo.AddAsync(refund);

        var queryHandler = new GetBusinessDaySummaryQueryHandler(
            _dayCloseRepo,
            _shiftRepo,
            _paymentRepo,
            _paymentMethodRepo,
            _dateProvider,
            _orderRepo,
            _orderLineRepo,
            _refundRepo);

        var summary = await queryHandler.Handle(new GetBusinessDaySummaryQuery(_branchId, _businessDate), CancellationToken.None);

        Assert.True(summary.CanClose);
        Assert.Equal(1044.00m, summary.TotalSales);
        Assert.Equal(1044.00m, summary.CashSales);
        Assert.Equal(144.00m, summary.Tax);
        Assert.Equal(100.00m, summary.Discounts);
        Assert.Equal(522.00m, summary.Refunds);
    }
}
