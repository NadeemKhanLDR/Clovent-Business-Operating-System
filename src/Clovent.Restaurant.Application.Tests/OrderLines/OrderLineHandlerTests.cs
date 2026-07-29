using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.OrderLines;

public class OrderLineHandlerTests
{
    private static FakeMediator CreateCatalogMediator(Guid productVariantId, Guid productId, decimal price, decimal taxRate, bool taxInclusive) => new(request =>
    {
        object? response = request switch
        {
            GetProductVariantByIdQuery => new ProductVariantDto(productVariantId, productId, "Standard", "SKU-1", Guid.NewGuid(), "Active", DateTimeOffset.UtcNow),
            GetProductByIdQuery => new ProductDto(productId, "Cola", "COLA-1", null, null, null, Guid.NewGuid(), taxRate, taxInclusive, "Active", DateTimeOffset.UtcNow),
            ListProductPricesByVariantQuery => (IReadOnlyCollection<ProductPriceDto>)
                [new ProductPriceDto(Guid.NewGuid(), productVariantId, "Selling", price, Guid.NewGuid(), DateTimeOffset.UtcNow, "Active", DateTimeOffset.UtcNow)],
            _ => throw new NotSupportedException(request.GetType().Name),
        };
        return Task.FromResult<object?>(response);
    });

    [Fact]
    public async Task AddOrderLineCommandHandler_ResolvesPriceAndTaxFromCatalog()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var order = Order.Create(OrderType.TakeAway, MasterData.Warehouses.WarehouseId.New());
        orderRepository.Add(order);

        var productVariantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var mediator = CreateCatalogMediator(productVariantId, productId, 9.99m, 15m, false);

        var handler = new AddOrderLineCommandHandler(orderRepository, orderLineRepository, mediator);

        var result = await handler.Handle(new AddOrderLineCommand(order.Id.Value, productVariantId, 2, "No ice"), CancellationToken.None);

        Assert.Equal(9.99m, result.UnitPrice);
        Assert.Equal(15m, result.TaxRatePercentage);
        Assert.Equal("No ice", result.Notes);
        Assert.Contains(new OrderLineId(result.OrderLineId), order.OrderLineIds);
    }

    [Fact]
    public async Task SetOrderLineQuantityCommandHandler_Changes()
    {
        var repository = new FakeOrderLineRepository();
        var line = OrderLine.Create(OrderId.New(), Clovent.Catalog.Variants.ProductVariantId.New(), 1, 5m, 0, false);
        repository.Add(line);

        var result = await new SetOrderLineQuantityCommandHandler(repository).Handle(new SetOrderLineQuantityCommand(line.Id.Value, 3), CancellationToken.None);

        Assert.Equal(3, result.Quantity);
    }

    [Fact]
    public async Task VoidThenUnvoid_RoundTrips()
    {
        var repository = new FakeOrderLineRepository();
        var line = OrderLine.Create(OrderId.New(), Clovent.Catalog.Variants.ProductVariantId.New(), 1, 5m, 0, false);
        repository.Add(line);

        var voided = await new VoidOrderLineCommandHandler(repository).Handle(new VoidOrderLineCommand(line.Id.Value), CancellationToken.None);
        Assert.True(voided.IsVoided);

        var unvoided = await new UnvoidOrderLineCommandHandler(repository).Handle(new UnvoidOrderLineCommand(line.Id.Value), CancellationToken.None);
        Assert.False(unvoided.IsVoided);
    }

    [Fact]
    public async Task RemoveOrderLineCommandHandler_DetachesFromOrderAndVoids()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var order = Order.Create(OrderType.TakeAway, MasterData.Warehouses.WarehouseId.New());
        var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 5m, 0, false);
        order.AddOrderLine(line.Id);
        orderRepository.Add(order);
        orderLineRepository.Add(line);

        var result = await new RemoveOrderLineCommandHandler(orderRepository, orderLineRepository)
            .Handle(new RemoveOrderLineCommand(order.Id.Value, line.Id.Value), CancellationToken.None);

        Assert.True(result.IsVoided);
        Assert.DoesNotContain(line.Id, order.OrderLineIds);
    }

    [Fact]
    public async Task ListOrderLinesByOrderQueryHandler_FiltersToOrder()
    {
        var repository = new FakeOrderLineRepository();
        var orderId = OrderId.New();
        repository.Add(OrderLine.Create(orderId, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 5m, 0, false));
        repository.Add(OrderLine.Create(orderId, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 5m, 0, false));
        repository.Add(OrderLine.Create(OrderId.New(), Clovent.Catalog.Variants.ProductVariantId.New(), 1, 5m, 0, false));

        var result = await new ListOrderLinesByOrderQueryHandler(repository).Handle(new ListOrderLinesByOrderQuery(orderId.Value), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
