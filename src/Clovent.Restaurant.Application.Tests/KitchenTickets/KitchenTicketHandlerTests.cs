using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.KitchenTickets.Commands;
using Clovent.Restaurant.Application.KitchenTickets.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.KitchenTickets;

public class KitchenTicketHandlerTests
{
    [Fact]
    public async Task SendOrderToKitchenCommandHandler_OnlyIncludesActiveLines()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var kitchenTicketRepository = new FakeKitchenTicketRepository();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var activeLine = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 5m, 0, false);
        var voidedLine = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 5m, 0, false);
        voidedLine.Void();
        order.AddOrderLine(activeLine.Id);
        order.AddOrderLine(voidedLine.Id);
        orderRepository.Add(order);
        orderLineRepository.Add(activeLine);
        orderLineRepository.Add(voidedLine);

        var handler = new SendOrderToKitchenCommandHandler(orderRepository, orderLineRepository, kitchenTicketRepository);
        var result = await handler.Handle(new SendOrderToKitchenCommand(order.Id.Value), CancellationToken.None);

        Assert.Contains(activeLine.Id.Value, result.OrderLineIds);
        Assert.DoesNotContain(voidedLine.Id.Value, result.OrderLineIds);
        Assert.Equal("New", result.Status);
    }

    [Fact]
    public async Task StartMarkReadyServe_FullLifecycle()
    {
        var repository = new FakeKitchenTicketRepository();
        var ticket = KitchenTicket.Create(OrderId.New(), [OrderLineId.New()]);
        repository.Add(ticket);

        var started = await new StartKitchenTicketCommandHandler(repository).Handle(new StartKitchenTicketCommand(ticket.Id.Value), CancellationToken.None);
        Assert.Equal("InProgress", started.Status);

        var ready = await new MarkKitchenTicketReadyCommandHandler(repository).Handle(new MarkKitchenTicketReadyCommand(ticket.Id.Value), CancellationToken.None);
        Assert.Equal("Ready", ready.Status);

        var served = await new ServeKitchenTicketCommandHandler(repository).Handle(new ServeKitchenTicketCommand(ticket.Id.Value), CancellationToken.None);
        Assert.Equal("Served", served.Status);
    }

    [Fact]
    public async Task CancelKitchenTicketCommandHandler_FromNew_Cancels()
    {
        var repository = new FakeKitchenTicketRepository();
        var ticket = KitchenTicket.Create(OrderId.New(), [OrderLineId.New()]);
        repository.Add(ticket);

        var result = await new CancelKitchenTicketCommandHandler(repository).Handle(new CancelKitchenTicketCommand(ticket.Id.Value), CancellationToken.None);

        Assert.Equal("Cancelled", result.Status);
    }

    [Fact]
    public async Task ListActiveKitchenTicketsQueryHandler_ExcludesServedAndCancelled()
    {
        var repository = new FakeKitchenTicketRepository();
        var active = KitchenTicket.Create(OrderId.New(), [OrderLineId.New()]);
        var served = KitchenTicket.Create(OrderId.New(), [OrderLineId.New()]);
        served.Start();
        served.MarkReady();
        served.Serve();
        repository.Add(active);
        repository.Add(served);

        var result = await new ListActiveKitchenTicketsQueryHandler(repository).Handle(new ListActiveKitchenTicketsQuery(), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task ListKitchenTicketsByOrderQueryHandler_FiltersToOrder()
    {
        var repository = new FakeKitchenTicketRepository();
        var orderId = OrderId.New();
        repository.Add(KitchenTicket.Create(orderId, [OrderLineId.New()]));
        repository.Add(KitchenTicket.Create(OrderId.New(), [OrderLineId.New()]));

        var result = await new ListKitchenTicketsByOrderQueryHandler(repository).Handle(new ListKitchenTicketsByOrderQuery(orderId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
