using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.EndOfDay;

public class GetExpandedSalesSummaryQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithDuplicateActivePrices_DoesNotThrowAndAggregatesProperly()
    {
        // Arrange
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var paymentRepository = new FakePaymentRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var customerRepository = new FakeCustomerRepository();
        var customerLedgerRepository = new FakeCustomerLedgerEntryRepository();
        var discountRepository = new FakeDiscountRepository();
        var serviceChargeRepository = new FakeServiceChargeRepository();
        var shiftRepository = new FakeShiftRepository();
        var tableRepository = new FakeTableRepository();

        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var productId = Guid.NewGuid();

        var order = Order.Create(OrderType.TakeAway, warehouseId);
        var line = OrderLine.Create(order.Id, variantId, 2, 500m, 0, false);
        order.AddOrderLine(line.Id);
        orderLineRepository.Add(line);

        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(method);
        var payment = Payment.Create(order.Id, method.Id, 1000m);
        order.RecordPayment(payment.Id);
        paymentRepository.Add(payment);

        order.Complete();
        orderRepository.Add(order);

        // Setup fake catalog mediator returning duplicate active prices for the same variant
        var mockPrices = new List<ProductPriceDto>
        {
            new(Guid.NewGuid(), variantId.Value, "Cost", 300m, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-10), "Active", DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), variantId.Value, "Cost", 320m, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-5), "Active", DateTimeOffset.UtcNow)
        };

        var mockVariants = new List<ProductVariantDto>
        {
            CatalogFakes.Variant(variantId.Value, productId, "Single")
        };

        var mockProducts = new List<ProductDto>
        {
            CatalogFakes.Product(productId, "Chicken Biryani")
        };

        var fakeMediator = new FakeMediator(req =>
        {
            if (req is ListActiveProductPricesByTypeQuery q && q.PriceType == Clovent.Catalog.Prices.PriceType.Cost)
            {
                return Task.FromResult<object?>(mockPrices);
            }
            if (req is ListActiveProductPricesByTypeQuery)
            {
                return Task.FromResult<object?>(new List<ProductPriceDto>());
            }
            if (req is ListProductVariantsQuery)
            {
                return Task.FromResult<object?>(mockVariants);
            }
            if (req is ListProductsQuery)
            {
                return Task.FromResult<object?>(mockProducts);
            }
            return Task.FromResult<object?>(null);
        });

        var handler = new GetExpandedSalesSummaryQueryHandler(
            orderRepository,
            orderLineRepository,
            paymentRepository,
            paymentMethodRepository,
            customerRepository,
            customerLedgerRepository,
            discountRepository,
            serviceChargeRepository,
            shiftRepository,
            tableRepository,
            fakeMediator);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = await handler.Handle(new GetExpandedSalesSummaryQuery(warehouseId.Value, today, today), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Orders);
        Assert.Equal(1000m, result.Kpis.GrossSales);
        Assert.Equal(1000m, result.Kpis.CashCollected);
        Assert.Single(result.Items);
        Assert.Equal("Chicken Biryani", result.Items[0].ItemName);
        Assert.Equal(1000m, result.Items[0].TotalSales);
        Assert.NotNull(result.Items[0].EstimatedCost);
    }

    [Fact]
    public async Task Handle_OnAccountOrder_MapsCustomerNameAndAvoidsHardcodedGuest()
    {
        // Arrange
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var paymentRepository = new FakePaymentRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var customerRepository = new FakeCustomerRepository();
        var customerLedgerRepository = new FakeCustomerLedgerEntryRepository();
        var discountRepository = new FakeDiscountRepository();
        var serviceChargeRepository = new FakeServiceChargeRepository();
        var shiftRepository = new FakeShiftRepository();
        var tableRepository = new FakeTableRepository();

        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();

        var customer = Customer.Create(
            EntityCode.Create("C005"),
            "Corporate Lunch Account",
            "0300123456",
            "Business District",
            null,
            0m,
            50000m,
            null);
        await customerRepository.AddAsync(customer);

        var order = Order.Create(OrderType.TakeAway, warehouseId);
        order.SetCustomer(customer.Id);
        var line = OrderLine.Create(order.Id, variantId, 1, 1500m, 0, false);
        order.AddOrderLine(line.Id);
        orderLineRepository.Add(line);

        // Order completed on account without immediate settlement payment
        order.Complete();
        orderRepository.Add(order);

        var fakeMediator = new FakeMediator(req =>
        {
            if (req is ListActiveProductPricesByTypeQuery)
                return Task.FromResult<object?>(new List<ProductPriceDto>());
            if (req is ListProductVariantsQuery)
                return Task.FromResult<object?>(new List<ProductVariantDto>());
            if (req is ListProductsQuery)
                return Task.FromResult<object?>(new List<ProductDto>());
            return Task.FromResult<object?>(null);
        });

        var handler = new GetExpandedSalesSummaryQueryHandler(
            orderRepository,
            orderLineRepository,
            paymentRepository,
            paymentMethodRepository,
            customerRepository,
            customerLedgerRepository,
            discountRepository,
            serviceChargeRepository,
            shiftRepository,
            tableRepository,
            fakeMediator);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = await handler.Handle(new GetExpandedSalesSummaryQuery(warehouseId.Value, today, today), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Orders);
        Assert.Equal("Corporate Lunch Account", result.Orders[0].CustomerName);
        Assert.Equal(1500m, result.Orders[0].OnAccountAmount);
        Assert.Equal(1500m, result.Kpis.OnAccountCreated);
    }
}
