using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
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
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Orders;

public class CompleteOrderCommandHandlerTests
{
    private static FakeMediator CreateInventoryMediator(Guid warehouseStockId, List<(Guid WarehouseStockId, decimal Quantity)> issuedCalls) => new(request =>
    {
        object? response = request switch
        {
            GetWarehouseStockByWarehouseAndVariantQuery => new WarehouseStockDto(warehouseStockId, Guid.NewGuid(), Guid.NewGuid(), 100, 0, 100, 0, 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            IssueStockCommand issue => Record(issue, issuedCalls),
            _ => throw new NotSupportedException(request.GetType().Name),
        };
        return Task.FromResult<object?>(response);
    });

    private static WarehouseStockDto Record(IssueStockCommand command, List<(Guid, decimal)> calls)
    {
        calls.Add((command.WarehouseStockId, command.Quantity));
        return new WarehouseStockDto(command.WarehouseStockId, Guid.NewGuid(), Guid.NewGuid(), 100 - command.Quantity, 0, 100 - command.Quantity, 0, 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_FullyPaid_CompletesAndIssuesStock()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var discountRepository = new FakeDiscountRepository();
        var serviceChargeRepository = new FakeServiceChargeRepository();
        var paymentRepository = new FakePaymentRepository();
        var tableRepository = new FakeTableRepository();
        var dailySalesSequenceRepository = new FakeDailySalesSequenceRepository();

        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        table.Occupy();
        tableRepository.Add(table);

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 2, 10m, 0, false);
        order.AddOrderLine(line.Id);
        var payment = Payment.Create(order.Id, PaymentMethodId.New(), 20m);
        order.RecordPayment(payment.Id);

        orderRepository.Add(order);
        orderLineRepository.Add(line);
        paymentRepository.Add(payment);

        var warehouseStockId = Guid.NewGuid();
        var issuedCalls = new List<(Guid, decimal)>();
        var mediator = CreateInventoryMediator(warehouseStockId, issuedCalls);

        var handler = new CompleteOrderCommandHandler(orderRepository, orderLineRepository, discountRepository, serviceChargeRepository, paymentRepository, tableRepository, dailySalesSequenceRepository, mediator);

        var result = await handler.Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.Equal("Available", table.OccupancyStatus.ToString());
        Assert.Single(issuedCalls);
        Assert.Equal((warehouseStockId, 2m), issuedCalls[0]);
        Assert.Equal(1, result.DailySalesNumber);
    }

    [Fact]
    public async Task Handle_NotFullyPaid_Throws()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var discountRepository = new FakeDiscountRepository();
        var serviceChargeRepository = new FakeServiceChargeRepository();
        var paymentRepository = new FakePaymentRepository();
        var tableRepository = new FakeTableRepository();
        var dailySalesSequenceRepository = new FakeDailySalesSequenceRepository();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 50m, 0, false);
        order.AddOrderLine(line.Id);
        orderRepository.Add(order);
        orderLineRepository.Add(line);

        var mediator = CreateInventoryMediator(Guid.NewGuid(), []);
        var handler = new CompleteOrderCommandHandler(orderRepository, orderLineRepository, discountRepository, serviceChargeRepository, paymentRepository, tableRepository, dailySalesSequenceRepository, mediator);

        await Assert.ThrowsAsync<RestaurantDomainException>(() => handler.Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None));
    }
}
