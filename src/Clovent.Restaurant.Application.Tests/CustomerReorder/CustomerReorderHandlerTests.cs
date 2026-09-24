using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.CustomerReorder.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.CustomerReorder;

public class CustomerReorderHandlerTests
{
    private readonly Guid _pizzaProductId = Guid.NewGuid();
    private readonly Guid _burgerProductId = Guid.NewGuid();
    private readonly Guid _pastaVariantId = Guid.NewGuid();

    private readonly Guid _pizzaVariantId = Guid.NewGuid();
    private readonly Guid _burgerVariantId = Guid.NewGuid();
    private readonly Guid _discontinuedVariantId = Guid.NewGuid();

    private readonly FakeCustomerRepository _customerRepository = new();
    private readonly FakeOrderRepository _orderRepository = new();
    private readonly FakeOrderLineRepository _orderLineRepository = new();

    private Customer _customer = null!;

    private MediatR.IMediator CreateMediator() =>
        CatalogFakes.Mediator(
            new[]
            {
                CatalogFakes.Variant(_pizzaVariantId, _pizzaProductId, "Margherita"),
                CatalogFakes.Variant(_burgerVariantId, _burgerProductId, "Beef Burger"),
                CatalogFakes.Variant(_discontinuedVariantId, _burgerProductId, "Chicken Burger", status: "Inactive"),
                CatalogFakes.Variant(_pastaVariantId, Guid.NewGuid(), "Alfredo"),
            },
            new[]
            {
                CatalogFakes.Product(_pizzaProductId, "Pizza"),
                CatalogFakes.Product(_burgerProductId, "Burger"),
            },
            new[]
            {
                CatalogFakes.SellingPrice(_pizzaVariantId, 900m),
                CatalogFakes.SellingPrice(_burgerVariantId, 500m),
                CatalogFakes.SellingPrice(_discontinuedVariantId, 450m),
                CatalogFakes.SellingPrice(_pastaVariantId, 700m),
            });

    private async Task<Customer> CreateCustomerAsync()
    {
        _customer = Customer.Create(EntityCode.Create("CUST-500"), "Regular Rita", "0300555444", "Street 1", null, 0m, 0m, null);
        await _customerRepository.AddAsync(_customer);
        return _customer;
    }

    private async Task<Order> AddCompletedOrderForCustomerAsync(Customer customer, params (Guid VariantId, decimal Quantity, decimal UnitPrice)[] lines)
    {
        var order = Order.Create(OrderType.TakeAway, new WarehouseId(Guid.NewGuid()));
        order.SetCustomer(customer.Id);
        foreach (var (variantId, quantity, unitPrice) in lines)
        {
            var line = OrderLine.Create(order.Id, new ProductVariantId(variantId), quantity, unitPrice, 0m, true);
            order.AddOrderLine(line.Id);
            await _orderLineRepository.AddAsync(line);
        }

        order.Complete();
        await _orderRepository.AddAsync(order);
        return order;
    }

    [Fact]
    public async Task LastOrder_ReturnsMostRecent_WithAvailabilityAndCurrentPrices()
    {
        var customer = await CreateCustomerAsync();
        await AddCompletedOrderForCustomerAsync(customer, (_pizzaVariantId, 1m, 850m));
        Thread.Sleep(20); // guarantee a strictly later UpdatedAtUtc
        await AddCompletedOrderForCustomerAsync(customer, (_burgerVariantId, 2m, 480m), (_discontinuedVariantId, 1m, 450m));

        var handler = new GetCustomerLastOrderQueryHandler(_customerRepository, _orderRepository, _orderLineRepository, CreateMediator());
        var result = await handler.Handle(new GetCustomerLastOrderQuery(customer.Id.Value), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Lines.Count);

        var burger = result.Lines.Single(l => l.VariantId == _burgerVariantId);
        Assert.Equal("Burger", burger.ProductName);
        Assert.Equal(2m, burger.Quantity);
        Assert.Equal(500m, burger.UnitPrice); // today's price, not the 480 snapshot
        Assert.True(burger.IsAvailable);

        var discontinued = result.Lines.Single(l => l.VariantId == _discontinuedVariantId);
        Assert.False(discontinued.IsAvailable);
    }

    [Fact]
    public async Task LastOrder_CustomerWithoutOrders_ReturnsNull()
    {
        var customer = await CreateCustomerAsync();

        var handler = new GetCustomerLastOrderQueryHandler(_customerRepository, _orderRepository, _orderLineRepository, CreateMediator());
        var result = await handler.Handle(new GetCustomerLastOrderQuery(customer.Id.Value), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FrequentProducts_OrderedByOrderCountThenRecency_InactiveExcluded()
    {
        var customer = await CreateCustomerAsync();
        await AddCompletedOrderForCustomerAsync(customer, (_pizzaVariantId, 1m, 900m));
        Thread.Sleep(20);
        await AddCompletedOrderForCustomerAsync(customer, (_pizzaVariantId, 1m, 900m), (_discontinuedVariantId, 1m, 450m));
        Thread.Sleep(20);
        await AddCompletedOrderForCustomerAsync(customer, (_burgerVariantId, 1m, 500m), (_discontinuedVariantId, 1m, 450m));

        var handler = new GetCustomerFrequentProductsQueryHandler(_customerRepository, _orderRepository, _orderLineRepository, CreateMediator());
        var results = await handler.Handle(new GetCustomerFrequentProductsQuery(customer.Id.Value, 5), CancellationToken.None);

        // Pizza appeared in 2 orders, burger in 1, discontinued (inactive) excluded.
        Assert.Equal([_pizzaVariantId, _burgerVariantId], [.. results.Select(l => l.VariantId)]);
        Assert.Equal(2m, results.ToList()[0].Quantity);
        Assert.Equal(1m, results.ToList()[1].Quantity);
        Assert.All(results, l => Assert.True(l.IsAvailable));
    }

    [Fact]
    public async Task FrequentProducts_TakeLimits()
    {
        var customer = await CreateCustomerAsync();
        await AddCompletedOrderForCustomerAsync(customer, (_pizzaVariantId, 1m, 900m), (_burgerVariantId, 1m, 500m), (_pastaVariantId, 1m, 700m));

        var handler = new GetCustomerFrequentProductsQueryHandler(_customerRepository, _orderRepository, _orderLineRepository, CreateMediator());
        var results = await handler.Handle(new GetCustomerFrequentProductsQuery(customer.Id.Value, 2), CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

}
