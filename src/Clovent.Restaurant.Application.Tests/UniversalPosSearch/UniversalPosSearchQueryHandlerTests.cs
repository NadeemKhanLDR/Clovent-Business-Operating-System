using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Identity.Branches;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Application.UniversalPosSearch.Queries;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.UniversalPosSearch;

public class UniversalPosSearchQueryHandlerTests
{
    private readonly Guid _pizzaProductId = Guid.NewGuid();
    private readonly Guid _pizzaVariantId = Guid.NewGuid();
    private readonly Guid _mainCourseCategoryId = Guid.NewGuid();

    private readonly FakeCustomerRepository _customerRepository = new();
    private readonly FakeOrderRepository _orderRepository = new();
    private readonly FakeOrderLineRepository _orderLineRepository = new();
    private readonly FakeTableRepository _tableRepository = new();
    private readonly FakeDiningAreaRepository _diningAreaRepository = new();

    private UniversalPosSearchQueryHandler CreateHandler() =>
        new(
            _customerRepository,
            _orderRepository,
            _orderLineRepository,
            _tableRepository,
            _diningAreaRepository,
            CatalogFakes.Mediator(
                new[] { CatalogFakes.Variant(_pizzaVariantId, _pizzaProductId, "Margherita", sortOrder: 1) },
                new[] { CatalogFakes.Product(_pizzaProductId, "Pizza", _mainCourseCategoryId) },
                new[] { CatalogFakes.SellingPrice(_pizzaVariantId, 900m) },
                new[] { CatalogFakes.Category(_mainCourseCategoryId, "Main Course") }));

    [Fact]
    public async Task Search_ByProductName_ReturnsVariantWithPriceAndCategory()
    {
        var results = await CreateHandler().Handle(new UniversalPosSearchQuery("pizza"), CancellationToken.None);

        var product = Assert.Single(results.Products);
        Assert.Equal(_pizzaVariantId, product.VariantId);
        Assert.Equal("Pizza", product.ProductName);
        Assert.Equal("Margherita", product.VariantName);
        Assert.Equal(900m, product.UnitPrice);
        Assert.Equal("Main Course", product.CategoryName);
    }

    [Fact]
    public async Task Search_ByCustomerCode_Name_AndPhone_ReturnsCustomer()
    {
        var customer = Customer.Create(
            EntityCode.Create("CUST-777"), "Ahmed Raza", "03001234567", "Main Street", null, 0m, 0m, null, phone: "0511112222");
        await _customerRepository.AddAsync(customer);
        var handler = CreateHandler();

        var byCode = await handler.Handle(new UniversalPosSearchQuery("777"), CancellationToken.None);
        var byName = await handler.Handle(new UniversalPosSearchQuery("raza"), CancellationToken.None);
        var byPhone = await handler.Handle(new UniversalPosSearchQuery("0300123"), CancellationToken.None);

        Assert.All(new[] { byCode, byName, byPhone }, r =>
        {
            var match = Assert.Single(r.Customers);
            Assert.Equal(customer.Id.Value, match.CustomerId);
            Assert.Equal("CUST-777", match.Code);
        });
    }

    [Fact]
    public async Task Search_ByOrderNumber_ReturnsOrderWithTotal()
    {
        var order = Order.Create(OrderType.TakeAway, new WarehouseId(Guid.NewGuid()));
        var line = OrderLine.Create(order.Id, new ProductVariantId(_pizzaVariantId), 2m, 900m, 0m, true);
        order.AddOrderLine(line.Id);
        await _orderLineRepository.AddAsync(line);
        await _orderRepository.AddAsync(order);

        var suffix = order.OrderNumber.Value[^3..];
        var results = await CreateHandler().Handle(new UniversalPosSearchQuery(suffix), CancellationToken.None);

        var match = Assert.Single(results.Orders);
        Assert.Equal(order.Id.Value, match.OrderId);
        Assert.Equal(order.OrderNumber.Value, match.OrderNumber);
        Assert.Equal(1800m, match.TotalAmount);
    }

    [Fact]
    public async Task Search_ByTableName_ReturnsTableWithAreaAndOpenOrderFlag()
    {
        var area = DiningArea.Create(new BranchId(Guid.NewGuid()), DiningAreaName.Create("Terrace"));
        var table = Table.Create(area.Id, EntityCode.Create("T-05"), 4);
        _diningAreaRepository.Add(area);
        await _tableRepository.AddAsync(table);

        // An open dine-in order on the table marks HasOpenOrder.
        var dineIn = Order.Create(OrderType.DineIn, new WarehouseId(Guid.NewGuid()), table.Id);
        await _orderRepository.AddAsync(dineIn);

        var results = await CreateHandler().Handle(new UniversalPosSearchQuery("T-05"), CancellationToken.None);

        var match = Assert.Single(results.Tables);
        Assert.Equal(table.Id.Value, match.TableId);
        Assert.Equal("Terrace", match.AreaName);
        Assert.True(match.HasOpenOrder);
    }

    [Fact]
    public async Task InactiveCustomer_IsNotFound()
    {
        var customer = Customer.Create(
            EntityCode.Create("CUST-888"), "Sleeper", "0300999888", "Old Street", null, 0m, 0m, null);
        customer.SetStatus(false);
        await _customerRepository.AddAsync(customer);

        var results = await CreateHandler().Handle(new UniversalPosSearchQuery("sleeper"), CancellationToken.None);

        Assert.Empty(results.Customers);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("p")]
    public async Task TermShorterThanTwoCharacters_ReturnsEmpty(string term)
    {
        var results = await CreateHandler().Handle(new UniversalPosSearchQuery(term), CancellationToken.None);

        Assert.Empty(results.Products);
        Assert.Empty(results.Customers);
        Assert.Empty(results.Orders);
        Assert.Empty(results.Tables);
    }
}
