using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.EndOfDay;

public class GetEndOfDayReportQueryHandlerTests
{
    private static (Order Order, PaymentMethod CashMethod) CreateCompletedOrder(
        FakeOrderRepository orderRepository,
        FakeOrderLineRepository orderLineRepository,
        FakePaymentRepository paymentRepository,
        FakePaymentMethodRepository paymentMethodRepository,
        WarehouseId warehouseId,
        ProductVariantId variantId,
        decimal quantity,
        decimal unitPrice,
        PaymentMethod? cashMethod = null)
    {
        var order = Order.Create(OrderType.TakeAway, warehouseId);
        var line = OrderLine.Create(order.Id, variantId, quantity, unitPrice, 0, false);
        order.AddOrderLine(line.Id);
        orderLineRepository.Add(line);

        var method = cashMethod ?? PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(method);
        var payment = Payment.Create(order.Id, method.Id, quantity * unitPrice);
        order.RecordPayment(payment.Id);
        paymentRepository.Add(payment);

        order.Complete();
        orderRepository.Add(order);

        return (order, method);
    }

    [Fact]
    public async Task Handle_CompletedOrderToday_AggregatesSalesAndCashCollected()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var paymentRepository = new FakePaymentRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();

        CreateCompletedOrder(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository, warehouseId, variantId, 3, 10m);

        var handler = new GetEndOfDayReportQueryHandler(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var report = await handler.Handle(new GetEndOfDayReportQuery(warehouseId.Value, today, today), CancellationToken.None);

        Assert.Equal(30m, report.TotalSales);
        Assert.Equal(30m, report.CashCollected);
        Assert.Equal(1, report.ReceiptCount);
        Assert.Equal(30m, report.AverageSale);
        Assert.Single(report.ItemsSold);
        Assert.Equal(3m, report.ItemsSold[0].Quantity);
        Assert.Single(report.CashSummary);
        Assert.Equal("Cash", report.CashSummary[0].PaymentMethodName);
    }

    [Fact]
    public async Task Handle_OrderAtDifferentWarehouse_Excluded()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var paymentRepository = new FakePaymentRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        CreateCompletedOrder(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository, WarehouseId.New(), ProductVariantId.New(), 1, 5m);

        var handler = new GetEndOfDayReportQueryHandler(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var report = await handler.Handle(new GetEndOfDayReportQuery(WarehouseId.New().Value, today, today), CancellationToken.None);

        Assert.Equal(0m, report.TotalSales);
        Assert.Equal(0, report.ReceiptCount);
        Assert.Empty(report.ItemsSold);
    }

    [Fact]
    public async Task Handle_VoidedOrderToday_CountedSeparatelyFromReceiptCount()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var paymentRepository = new FakePaymentRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var warehouseId = WarehouseId.New();

        var voidedOrder = Order.Create(OrderType.TakeAway, warehouseId);
        voidedOrder.Void("Customer changed their mind");
        orderRepository.Add(voidedOrder);

        var handler = new GetEndOfDayReportQueryHandler(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var report = await handler.Handle(new GetEndOfDayReportQuery(warehouseId.Value, today, today), CancellationToken.None);

        Assert.Equal(0, report.ReceiptCount);
        Assert.Equal(1, report.VoidedOrderCount);
    }

    [Fact]
    public async Task Handle_NonCashPaymentMethod_ExcludedFromCashCollectedButIncludedInTotalSales()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var paymentRepository = new FakePaymentRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var warehouseId = WarehouseId.New();
        var cardMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit Card"));

        CreateCompletedOrder(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository, warehouseId, ProductVariantId.New(), 1, 20m, cardMethod);

        var handler = new GetEndOfDayReportQueryHandler(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var report = await handler.Handle(new GetEndOfDayReportQuery(warehouseId.Value, today, today), CancellationToken.None);

        Assert.Equal(20m, report.TotalSales);
        Assert.Equal(0m, report.CashCollected);
        Assert.Equal(20m, report.CardCollected);
        Assert.Equal("Credit Card", report.CashSummary[0].PaymentMethodName);
    }

    [Fact]
    public async Task Handle_CashAndCardPayments_SplitsIntoBothTotalsCorrectly()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var paymentRepository = new FakePaymentRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var warehouseId = WarehouseId.New();
        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var debitCardMethod = PaymentMethod.Create(PaymentMethodName.Create("Debit Card"));

        CreateCompletedOrder(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository, warehouseId, ProductVariantId.New(), 1, 30m, cashMethod);
        CreateCompletedOrder(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository, warehouseId, ProductVariantId.New(), 1, 45m, debitCardMethod);

        var handler = new GetEndOfDayReportQueryHandler(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var report = await handler.Handle(new GetEndOfDayReportQuery(warehouseId.Value, today, today), CancellationToken.None);

        Assert.Equal(75m, report.TotalSales);
        Assert.Equal(30m, report.CashCollected);
        Assert.Equal(45m, report.CardCollected);
    }

    [Fact]
    public async Task Handle_DateRangeSpanningMultipleDays_SumsAllDaysInRange()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var paymentRepository = new FakePaymentRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var warehouseId = WarehouseId.New();
        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));

        CreateCompletedOrder(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository, warehouseId, ProductVariantId.New(), 1, 10m, cashMethod);
        CreateCompletedOrder(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository, warehouseId, ProductVariantId.New(), 1, 15m, cashMethod);

        var handler = new GetEndOfDayReportQueryHandler(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var singleDayReport = await handler.Handle(new GetEndOfDayReportQuery(warehouseId.Value, today, today), CancellationToken.None);
        var rangeReport = await handler.Handle(new GetEndOfDayReportQuery(warehouseId.Value, today.AddDays(-1), today), CancellationToken.None);

        Assert.Equal(25m, singleDayReport.TotalSales);
        Assert.Equal(25m, rangeReport.TotalSales);
        Assert.Equal(2, rangeReport.ReceiptCount);
    }

    [Fact]
    public async Task Handle_CompletedOrder_PopulatesOneBillRow()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var paymentRepository = new FakePaymentRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var warehouseId = WarehouseId.New();

        var (order, method) = CreateCompletedOrder(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository, warehouseId, ProductVariantId.New(), 2, 12.5m);

        var handler = new GetEndOfDayReportQueryHandler(orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var report = await handler.Handle(new GetEndOfDayReportQuery(warehouseId.Value, today, today), CancellationToken.None);

        var bill = Assert.Single(report.Bills);
        Assert.Equal(order.Id.Value, bill.OrderId);
        Assert.Equal(order.OrderNumber.Value, bill.OrderNumber);
        Assert.Equal(25m, bill.Total);
        Assert.Equal(method.Name.Value, bill.PaymentMethodSummary);
    }
}
