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
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using Clovent.Restaurant.Tables;
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

    [Fact]
    public async Task Handle_SeparatesItemSalesAndDeliveryFeesInKpis()
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

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cashMethod);

        // Order 1: DineIn, items = 1000, fee = 0
        var order1 = Order.Create(OrderType.DineIn, warehouseId, TableId.New());
        var line1 = OrderLine.Create(order1.Id, variantId, 2, 500m, 0, false);
        order1.AddOrderLine(line1.Id);
        orderLineRepository.Add(line1);
        var pay1 = Payment.Create(order1.Id, cashMethod.Id, 1000m);
        order1.RecordPayment(pay1.Id);
        paymentRepository.Add(pay1);
        order1.Complete();
        orderRepository.Add(order1);

        // Order 2: Delivery, items = 500, delivery fee = 100, total = 600
        var order2 = Order.Create(OrderType.Delivery, warehouseId);
        order2.SetDeliveryDetails(OrderSource.Phone, "Tariq Mahmood", "03001112233", "Address", null, 100m, "Rider Tariq");
        var line2 = OrderLine.Create(order2.Id, variantId, 1, 500m, 0, false);
        order2.AddOrderLine(line2.Id);
        orderLineRepository.Add(line2);
        var pay2 = Payment.Create(order2.Id, cashMethod.Id, 600m);
        order2.RecordPayment(pay2.Id);
        paymentRepository.Add(pay2);
        order2.Complete();
        orderRepository.Add(order2);

        var fakeMediator = new FakeMediator(req => Task.FromResult<object?>(null));

        var handler = new GetExpandedSalesSummaryQueryHandler(
            orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository,
            customerRepository, customerLedgerRepository, discountRepository, serviceChargeRepository,
            shiftRepository, tableRepository, fakeMediator);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = await handler.Handle(new GetExpandedSalesSummaryQuery(warehouseId.Value, today, today), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Kpis.TotalOrders);
        Assert.Equal(1500m, result.Kpis.ItemSalesValue); // Item sales: 1000 + 500
        Assert.Equal(100m, result.Kpis.DeliveryFees);    // Delivery fees: 100
        Assert.Equal(1600m, result.Kpis.TotalBillSalesValue); // Total bill sales: 1500 + 100
        Assert.Equal(800m, result.Kpis.AverageOrderValue);    // Average bill: 1600 / 2 = 800
        Assert.Equal(1600m, result.Kpis.CashCollected);
    }

    [Fact]
    public async Task Handle_OrderTypes_AggregatesPhysicalQuantitiesSoldMatchingItemsTab()
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
        var v1 = ProductVariantId.New();
        var v2 = ProductVariantId.New();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cashMethod);

        // Order 1: DineIn, 2 lines: qty 3 + qty 2 = 5 physical units
        var order1 = Order.Create(OrderType.DineIn, warehouseId, TableId.New());
        var l1 = OrderLine.Create(order1.Id, v1, 3, 100m, 0, false);
        var l2 = OrderLine.Create(order1.Id, v2, 2, 200m, 0, false);
        order1.AddOrderLine(l1.Id);
        order1.AddOrderLine(l2.Id);
        orderLineRepository.Add(l1);
        orderLineRepository.Add(l2);
        var p1 = Payment.Create(order1.Id, cashMethod.Id, 700m);
        order1.RecordPayment(p1.Id);
        paymentRepository.Add(p1);
        order1.Complete();
        orderRepository.Add(order1);

        // Order 2: TakeAway, 1 line: qty 4 physical units
        var order2 = Order.Create(OrderType.TakeAway, warehouseId);
        var l3 = OrderLine.Create(order2.Id, v1, 4, 100m, 0, false);
        order2.AddOrderLine(l3.Id);
        orderLineRepository.Add(l3);
        var p2 = Payment.Create(order2.Id, cashMethod.Id, 400m);
        order2.RecordPayment(p2.Id);
        paymentRepository.Add(p2);
        order2.Complete();
        orderRepository.Add(order2);

        // Order 3: Delivery, 1 line: qty 1 physical unit
        var order3 = Order.Create(OrderType.Delivery, warehouseId);
        order3.SetDeliveryDetails(OrderSource.Phone, "Cust", "0300", "Addr", null, 50m, "Rider");
        var l4 = OrderLine.Create(order3.Id, v2, 1, 200m, 0, false);
        order3.AddOrderLine(l4.Id);
        orderLineRepository.Add(l4);
        var p3 = Payment.Create(order3.Id, cashMethod.Id, 250m);
        order3.RecordPayment(p3.Id);
        paymentRepository.Add(p3);
        order3.Complete();
        orderRepository.Add(order3);

        var fakeMediator = new FakeMediator(req => Task.FromResult<object?>(null));

        var handler = new GetExpandedSalesSummaryQueryHandler(
            orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository,
            customerRepository, customerLedgerRepository, discountRepository, serviceChargeRepository,
            shiftRepository, tableRepository, fakeMediator);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = await handler.Handle(new GetExpandedSalesSummaryQuery(warehouseId.Value, today, today), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var dineIn = Assert.Single(result.OrderTypes, ot => ot.OrderType == "DineIn");
        var takeAway = Assert.Single(result.OrderTypes, ot => ot.OrderType == "TakeAway");
        var delivery = Assert.Single(result.OrderTypes, ot => ot.OrderType == "Delivery");

        Assert.Equal(5m, dineIn.QuantitySold);   // 3 + 2 = 5 units (NOT 2 lines!)
        Assert.Equal(4m, takeAway.QuantitySold); // 4 units
        Assert.Equal(1m, delivery.QuantitySold); // 1 unit

        decimal totalOrderTypesQty = result.OrderTypes.Sum(ot => ot.QuantitySold);
        decimal totalItemsTabQty = result.Items.Sum(i => i.QuantitySold);

        Assert.Equal(10m, totalOrderTypesQty);
        Assert.Equal(10m, totalItemsTabQty);
        Assert.Equal(totalItemsTabQty, totalOrderTypesQty); // 1:1 exact reconciliation!
    }

    [Fact]
    public async Task Handle_ReceivablesMovement_EnforcesStrictAccountingIdentity()
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
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        // Customer A: Prior advance = 70. Receives on-account sale of 1360. Closing A/R = 1290, Closing Adv = 0.
        var custA = Customer.Create(EntityCode.Create("C002"), "Waris Ali", "03001", "City", null, 0m, 50000m, null);
        await customerRepository.AddAsync(custA);
        // Prior entry before today
        await customerLedgerRepository.AddAsync(CustomerLedgerEntry.Create(
            custA.Id, "PAY-001", "Customer Payment", 0m, 70m, -70m, null, "Cash", now.AddDays(-2)));
        // Period debit: On account sale 1360
        await customerLedgerRepository.AddAsync(CustomerLedgerEntry.Create(
            custA.Id, "ORD-919", "On Account Sale (ORD-919) [70.00 Advance Applied]", 1360m, 0m, 1290m, null, "On Account", now));

        // Customer B: Opening 0. Credit sale 475, Payment 700 (475 applied, 225 adv created), Adv settlement 225. Closing 0, 0.
        var custB = Customer.Create(EntityCode.Create("CUST-RPT-D"), "Sana Textile", "03002", "City", null, 0m, 50000m, null);
        await customerRepository.AddAsync(custB);
        await customerLedgerRepository.AddAsync(CustomerLedgerEntry.Create(
            custB.Id, "ORD-920", "On Account Sale", 475m, 0m, 475m, null, "On Account", now.AddHours(-3)));
        await customerLedgerRepository.AddAsync(CustomerLedgerEntry.Create(
            custB.Id, "PAY-002", "Customer Payment (Cash) [475.00 applied, 225.00 advance]", 0m, 700m, -225m, null, "Cash", now.AddHours(-2)));
        await customerLedgerRepository.AddAsync(CustomerLedgerEntry.Create(
            custB.Id, "ADV-001", "Customer Advance Settlement", 225m, 0m, 0m, null, "Customer Advance", now.AddHours(-1)));

        // Customer C: Prior A/R = 130. Payment = 300 (130 applied, 170 adv created). Closing A/R = 0, Closing Adv = 170.
        var custC = Customer.Create(EntityCode.Create("CUST-CORP-01"), "Corporate Lunch", "03003", "City", null, 0m, 50000m, null);
        await customerRepository.AddAsync(custC);
        await customerLedgerRepository.AddAsync(CustomerLedgerEntry.Create(
            custC.Id, "ORD-921", "On Account Sale", 130m, 0m, 130m, null, "On Account", now.AddDays(-1)));
        await customerLedgerRepository.AddAsync(CustomerLedgerEntry.Create(
            custC.Id, "PAY-003", "Customer Payment (Cash) [130.00 applied, 170.00 advance]", 0m, 300m, -170m, null, "Cash", now));

        var fakeMediator = new FakeMediator(req => Task.FromResult<object?>(null));

        var handler = new GetExpandedSalesSummaryQueryHandler(
            orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository,
            customerRepository, customerLedgerRepository, discountRepository, serviceChargeRepository,
            shiftRepository, tableRepository, fakeMediator);

        // Act
        var result = await handler.Handle(new GetExpandedSalesSummaryQuery(warehouseId.Value, today, today), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Receivables.Count);

        foreach (var r in result.Receivables)
        {
            // Identity 1: Closing A/R = Opening A/R + New On-Account - Collections Applied - Advance Applied
            Assert.Equal(r.ClosingReceivable, r.OpeningReceivable + r.NewOnAccountSales - r.CustomerPayments - r.AdvanceApplied);
            // Identity 2: Closing Advance = Opening Advance + Advance Received - Advance Used
            Assert.Equal(r.ClosingAdvance, r.OpeningAdvance + r.AdvanceReceived - r.AdvanceUsed);
        }

        // Footer reconciliation
        decimal totalOpenAR = result.Receivables.Sum(r => r.OpeningReceivable);
        decimal totalNewOnAccount = result.Receivables.Sum(r => r.NewOnAccountSales);
        decimal totalCollections = result.Receivables.Sum(r => r.CustomerPayments);
        decimal totalAdvApplied = result.Receivables.Sum(r => r.AdvanceApplied);
        decimal totalCloseAR = result.Receivables.Sum(r => r.ClosingReceivable);

        decimal totalOpenAdv = result.Receivables.Sum(r => r.OpeningAdvance);
        decimal totalAdvRecv = result.Receivables.Sum(r => r.AdvanceReceived);
        decimal totalAdvUsed = result.Receivables.Sum(r => r.AdvanceUsed);
        decimal totalCloseAdv = result.Receivables.Sum(r => r.ClosingAdvance);

        Assert.Equal(totalCloseAR, totalOpenAR + totalNewOnAccount - totalCollections - totalAdvApplied);
        Assert.Equal(totalCloseAdv, totalOpenAdv + totalAdvRecv - totalAdvUsed);
    }

    [Fact]
    public async Task Handle_ShiftDrawers_ReconcilesCashDrawerCorrectly()
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
        var terminalId = TerminalId.New();
        var branchId = BranchId.New();
        var cashierId = UserId.New();

        var shift = Shift.Open(1003, branchId, warehouseId, terminalId, cashierId, "Hamza Cashier", 4000m);
        shift.AddCashMovement(CashMovementType.CashIn, 1000m, "Float Topup", cashierId);
        shift.AddCashMovement(CashMovementType.CashOut, 500m, "Supplies", cashierId);

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cashMethod);

        // Cash sales: 13,875
        var p = Payment.Create(OrderId.New(), cashMethod.Id, 13875m, shift.Id);
        paymentRepository.Add(p);

        // Cash collections: 2,800
        var entry = CustomerLedgerEntry.Create(
            CustomerId.New(), "PAY-004", "Customer Payment", 0m, 2800m, -2800m, shift.Id, "Cash", DateTimeOffset.UtcNow);
        await customerLedgerRepository.AddAsync(entry);

        // Close shift: Expected = 4000 + 1000 + 13875 + 2800 - 500 = 21175
        shift.Close(21175m, 21175m, null, null);
        await shiftRepository.AddAsync(shift);

        var fakeMediator = new FakeMediator(req => Task.FromResult<object?>(null));

        var handler = new GetExpandedSalesSummaryQueryHandler(
            orderRepository, orderLineRepository, paymentRepository, paymentMethodRepository,
            customerRepository, customerLedgerRepository, discountRepository, serviceChargeRepository,
            shiftRepository, tableRepository, fakeMediator);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = await handler.Handle(new GetExpandedSalesSummaryQuery(warehouseId.Value, today, today), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ShiftDrawers);
        var drawer = Assert.Single(result.ShiftDrawers);

        Assert.Equal(1003, drawer.ShiftNumber);
        Assert.Equal(4000m, drawer.OpeningFloat);
        Assert.Equal(13875m, drawer.CashSales);
        Assert.Equal(2800m, drawer.CashCollections);
        Assert.Equal(1000m, drawer.CashIn);
        Assert.Equal(500m, drawer.CashOut);
        Assert.Equal(21175m, drawer.ExpectedCash);
        Assert.Equal(21175m, drawer.CountedCash);
        Assert.Equal(0m, drawer.Variance);

        // Drawer identity: Opening + CashSales + CashCollections + CashIn - CashOut = ExpectedCash
        Assert.Equal(drawer.ExpectedCash, drawer.OpeningFloat + drawer.CashSales + drawer.CashCollections + drawer.CashIn - drawer.CashOut);
    }
}
