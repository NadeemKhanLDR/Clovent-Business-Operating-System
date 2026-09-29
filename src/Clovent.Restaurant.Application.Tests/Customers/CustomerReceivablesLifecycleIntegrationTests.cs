using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Customers;

public sealed class CustomerReceivablesLifecycleIntegrationTests
{
    [Fact]
    public async Task CustomerReceivables_FullLifecycle_MaintainsFIFOAllocationsAndLedgerBalances()
    {
        // Setup repositories
        var customerRepo = new FakeCustomerRepository();
        var ledgerRepo = new FakeCustomerLedgerEntryRepository();
        var orderRepo = new FakeOrderRepository();
        var orderLineRepo = new FakeOrderLineRepository();
        var paymentRepo = new FakePaymentRepository();
        var paymentMethodRepo = new FakePaymentMethodRepository();
        var allocationRepo = new FakeCustomerPaymentAllocationRepository();
        var serviceChargeRepo = new FakeServiceChargeRepository();
        var discountRepo = new FakeDiscountRepository();

        var warehouseId = new WarehouseId(Guid.NewGuid());
        var tableId = new TableId(Guid.NewGuid());

        // Seed Payment Methods: Cash and On Account
        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        await paymentMethodRepo.AddAsync(cashMethod);
        await paymentMethodRepo.AddAsync(onAccountMethod);

        // 1. Create Customer with Credit Allowed and 25,000 Credit Limit
        var createCustomerHandler = new CreateCustomerCommandHandler(customerRepo, ledgerRepo);
        var customerResult = await createCustomerHandler.Handle(
            new CreateCustomerCommand("CREDIT-DEMO", "Corporate Account Demo", "0300-1234567", "F-7 Islamabad", "demo@test.com", 0m, 25000m, "Milestone demo", IsCreditAllowed: true),
            CancellationToken.None);

        var customerId = new CustomerId(customerResult.CustomerId);
        var customer = await customerRepo.GetByIdAsync(customerId);
        Assert.NotNull(customer);
        Assert.True(customer.IsCreditAllowed);
        Assert.Equal(25000m, customer.CreditLimit);
        Assert.Equal(0m, customer.OutstandingBalance);

        // 2. Setup Handlers
        var recordPaymentHandler = new RecordPaymentCommandHandler(
            orderRepo,
            paymentRepo,
            customerRepo,
            ledgerRepo,
            paymentMethodRepo,
            orderLineRepo,
            discountRepo,
            serviceChargeRepo);

        var recordCustomerPaymentHandler = new RecordCustomerPaymentCommandHandler(
            customerRepo,
            ledgerRepo,
            orderRepo,
            paymentRepo,
            paymentMethodRepo,
            allocationRepo);

        // --- ORDER 1: Total 2,000 (500 Cash, 1,500 On Account) ---
        var order1 = Order.Create(OrderType.DineIn, warehouseId, tableId);
        order1.SetCustomer(customerId);
        var line1 = OrderLine.Create(order1.Id, new ProductVariantId(Guid.NewGuid()), 2m, 1000m, 0m, false, "Main Course Deal");
        order1.AddOrderLine(line1.Id);
        await orderLineRepo.AddAsync(line1);
        await orderRepo.AddAsync(order1);

        // Pay 500 Cash
        await recordPaymentHandler.Handle(
            new RecordPaymentCommand(order1.Id.Value, cashMethod.Id.Value, 500m),
            CancellationToken.None);

        // Pay 1,500 On Account
        await recordPaymentHandler.Handle(
            new RecordPaymentCommand(order1.Id.Value, onAccountMethod.Id.Value, 1500m),
            CancellationToken.None);

        // Verify Customer Outstanding Balance after Order 1
        customer = await customerRepo.GetByIdAsync(customerId);
        Assert.NotNull(customer);
        Assert.Equal(1500m, customer.OutstandingBalance);
        Assert.Equal(23500m, customer.AvailableCredit);

        // Verify Customer Ledger after Order 1
        var ledgerEntries = (await ledgerRepo.GetByCustomerIdAsync(customerId)).OrderBy(e => e.Date).ToList();
        var order1Entry = ledgerEntries.Last();
        Assert.Equal(1500m, order1Entry.Debit);
        Assert.Equal(0m, order1Entry.Credit);
        Assert.Equal(1500m, order1Entry.RunningBalance);

        // --- CUSTOMER PAYMENT 1: 600 Cash ---
        var payment1Result = await recordCustomerPaymentHandler.Handle(
            new RecordCustomerPaymentCommand(customerId.Value, 600m, "Cash", "REC-001", "Payment 1"),
            CancellationToken.None);

        Assert.Equal(1500m, payment1Result.OutstandingBefore);
        Assert.Equal(600m, payment1Result.AppliedAmount);
        Assert.Equal(900m, payment1Result.OutstandingAfter);

        // Verify Order 1 allocations: 600 allocated to Order 1
        var order1Allocations = await allocationRepo.GetByOrderIdAsync(order1.Id);
        Assert.Single(order1Allocations);
        Assert.Equal(600m, order1Allocations.First().Amount);

        // --- ORDER 2: Total 1,200 (1,200 On Account) ---
        var order2 = Order.Create(OrderType.TakeAway, warehouseId, null);
        order2.SetCustomer(customerId);
        var line2 = OrderLine.Create(order2.Id, new ProductVariantId(Guid.NewGuid()), 1m, 1200m, 0m, false, "Special Karahi Feast");
        order2.AddOrderLine(line2.Id);
        await orderLineRepo.AddAsync(line2);
        await orderRepo.AddAsync(order2);

        // Pay 1,200 On Account
        await recordPaymentHandler.Handle(
            new RecordPaymentCommand(order2.Id.Value, onAccountMethod.Id.Value, 1200m),
            CancellationToken.None);

        // Verify Customer Outstanding Balance: 900 + 1,200 = 2,100
        customer = await customerRepo.GetByIdAsync(customerId);
        Assert.NotNull(customer);
        Assert.Equal(2100m, customer.OutstandingBalance);
        Assert.Equal(22900m, customer.AvailableCredit);

        // --- CUSTOMER PAYMENT 2: 1,000 Cash ---
        // FIFO: 900 completes Order 1 (1,500 total), 100 allocated to Order 2
        var payment2Result = await recordCustomerPaymentHandler.Handle(
            new RecordCustomerPaymentCommand(customerId.Value, 1000m, "Cash", "REC-002", "Payment 2"),
            CancellationToken.None);

        Assert.Equal(2100m, payment2Result.OutstandingBefore);
        Assert.Equal(1000m, payment2Result.AppliedAmount);
        Assert.Equal(1100m, payment2Result.OutstandingAfter);

        // Verify Order 1 allocations: now 600 + 900 = 1,500 (fully settled!)
        order1Allocations = await allocationRepo.GetByOrderIdAsync(order1.Id);
        Assert.Equal(1500m, order1Allocations.Sum(a => a.Amount));

        // Verify Order 2 allocations: 100 allocated
        var order2Allocations = await allocationRepo.GetByOrderIdAsync(order2.Id);
        Assert.Single(order2Allocations);
        Assert.Equal(100m, order2Allocations.First().Amount);

        // --- CUSTOMER PAYMENT 3: 1,100 Cash ---
        // FIFO: completes Order 2 (100 + 1,100 = 1,200 total), balance becomes 0
        var payment3Result = await recordCustomerPaymentHandler.Handle(
            new RecordCustomerPaymentCommand(customerId.Value, 1100m, "Cash", "REC-003", "Payment 3"),
            CancellationToken.None);

        Assert.Equal(1100m, payment3Result.OutstandingBefore);
        Assert.Equal(1100m, payment3Result.AppliedAmount);
        Assert.Equal(0m, payment3Result.OutstandingAfter);

        // Verify Order 2 allocations: now 100 + 1,100 = 1,200 (fully settled!)
        order2Allocations = await allocationRepo.GetByOrderIdAsync(order2.Id);
        Assert.Equal(1200m, order2Allocations.Sum(a => a.Amount));

        // Verify final customer balance
        customer = await customerRepo.GetByIdAsync(customerId);
        Assert.NotNull(customer);
        Assert.Equal(0m, customer.OutstandingBalance);
        Assert.Equal(25000m, customer.AvailableCredit);

        // 3. Test Receivables Aging Report Query
        var receivablesQueryHandler = new GetCustomerReceivablesReportQueryHandler(
            customerRepo,
            ledgerRepo,
            orderRepo,
            paymentRepo,
            paymentMethodRepo,
            allocationRepo);

        var report = await receivablesQueryHandler.Handle(
            new GetCustomerReceivablesReportQuery(DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(0m, report.TotalReceivables);
        Assert.Equal(0m, report.TotalOverCreditLimit);
        Assert.Equal(0, report.ActiveAccountsWithBalanceCount);
    }

    [Fact]
    public async Task OnAccountPayment_WalkInCustomer_ThrowsInvalidOperationException()
    {
        var customerRepo = new FakeCustomerRepository();
        var ledgerRepo = new FakeCustomerLedgerEntryRepository();
        var orderRepo = new FakeOrderRepository();
        var orderLineRepo = new FakeOrderLineRepository();
        var paymentRepo = new FakePaymentRepository();
        var paymentMethodRepo = new FakePaymentMethodRepository();
        var serviceChargeRepo = new FakeServiceChargeRepository();
        var discountRepo = new FakeDiscountRepository();

        var warehouseId = new WarehouseId(Guid.NewGuid());
        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        await paymentMethodRepo.AddAsync(onAccountMethod);

        // Walk-in Customer C000 with IsDefault = true, IsCreditAllowed = false
        var walkInCustomer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C000"),
            "Walk-in Customer",
            "-",
            "Counter",
            null,
            0m,
            0m,
            "Default Walk-in");
        walkInCustomer.SetDefault(true);
        walkInCustomer.SetCreditAllowed(false);
        await customerRepo.AddAsync(walkInCustomer);

        var order = Order.Create(OrderType.TakeAway, warehouseId, null);
        order.SetCustomer(walkInCustomer.Id);
        var line = OrderLine.Create(order.Id, new ProductVariantId(Guid.NewGuid()), 1m, 500m, 0m, false, "Quick Item");
        order.AddOrderLine(line.Id);
        await orderLineRepo.AddAsync(line);
        await orderRepo.AddAsync(order);

        var handler = new RecordPaymentCommandHandler(
            orderRepo,
            paymentRepo,
            customerRepo,
            ledgerRepo,
            paymentMethodRepo,
            orderLineRepo,
            discountRepo,
            serviceChargeRepo);

        // Attempting to pay On Account for Walk-in Customer MUST fail
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new RecordPaymentCommand(order.Id.Value, onAccountMethod.Id.Value, 500m),
                CancellationToken.None));
    }

    [Fact]
    public async Task OnAccountPayment_ExceedingCreditLimitWithoutOverride_ThrowsInvalidOperationException()
    {
        var customerRepo = new FakeCustomerRepository();
        var ledgerRepo = new FakeCustomerLedgerEntryRepository();
        var orderRepo = new FakeOrderRepository();
        var orderLineRepo = new FakeOrderLineRepository();
        var paymentRepo = new FakePaymentRepository();
        var paymentMethodRepo = new FakePaymentMethodRepository();
        var serviceChargeRepo = new FakeServiceChargeRepository();
        var discountRepo = new FakeDiscountRepository();

        var warehouseId = new WarehouseId(Guid.NewGuid());
        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        await paymentMethodRepo.AddAsync(onAccountMethod);

        // Customer with credit allowed but low credit limit (1,000)
        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("CUST-LOW"),
            "Low Credit Customer",
            "0300-1111111",
            "Address",
            null,
            0m,
            1000m,
            null);
        customer.SetCreditAllowed(true);
        await customerRepo.AddAsync(customer);

        var order = Order.Create(OrderType.TakeAway, warehouseId, null);
        order.SetCustomer(customer.Id);
        var line = OrderLine.Create(order.Id, new ProductVariantId(Guid.NewGuid()), 1m, 1500m, 0m, false, "Expensive Item");
        order.AddOrderLine(line.Id);
        await orderLineRepo.AddAsync(line);
        await orderRepo.AddAsync(order);

        var handler = new RecordPaymentCommandHandler(
            orderRepo,
            paymentRepo,
            customerRepo,
            ledgerRepo,
            paymentMethodRepo,
            orderLineRepo,
            discountRepo,
            serviceChargeRepo);

        // Attempting to pay 1,500 with a 1,000 credit limit without override MUST fail
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new RecordPaymentCommand(order.Id.Value, onAccountMethod.Id.Value, 1500m, ExceedCreditLimitApproved: false),
                CancellationToken.None));

        // With override allowed, it must succeed
        var payment = await handler.Handle(
            new RecordPaymentCommand(order.Id.Value, onAccountMethod.Id.Value, 1500m, ExceedCreditLimitApproved: true),
            CancellationToken.None);

        Assert.NotNull(payment);
        Assert.Equal(1500m, payment.Amount);
    }

    [Fact]
    public async Task DeliveryOrder_CapturesDetails_AppliesFeeAndStatusTransitions()
    {
        var warehouseId = new WarehouseId(Guid.NewGuid());

        // Create Delivery Order
        var order = Order.Create(OrderType.Delivery, warehouseId, null);
        Assert.Equal(OrderType.Delivery, order.OrderType);
        Assert.Equal(DeliveryStatus.Received, order.DeliveryStatus);
        Assert.Equal(OrderSource.WalkIn, order.OrderSource);

        // Set Delivery Details
        order.SetDeliveryDetails(
            OrderSource.Phone,
            "Ali Ahmad",
            "0300-9876543",
            "House 12, Street 4, Sector F-8/1, Islamabad",
            "Ring bell twice, leave at door",
            100m,
            "Rider Kamran");

        Assert.Equal("Ali Ahmad", order.DeliveryCustomerName);
        Assert.Equal("0300-9876543", order.DeliveryPhone);
        Assert.Equal("House 12, Street 4, Sector F-8/1, Islamabad", order.DeliveryAddress);
        Assert.Equal("Ring bell twice, leave at door", order.DeliveryNotes);
        Assert.Equal(OrderSource.Phone, order.OrderSource);
        Assert.Equal(100m, order.DeliveryFee);
        Assert.Equal("Rider Kamran", order.RiderName);

        // Verify status lifecycle transitions
        order.UpdateDeliveryStatus(DeliveryStatus.Preparing);
        Assert.Equal(DeliveryStatus.Preparing, order.DeliveryStatus);

        order.UpdateDeliveryStatus(DeliveryStatus.Ready);
        Assert.Equal(DeliveryStatus.Ready, order.DeliveryStatus);

        order.UpdateDeliveryStatus(DeliveryStatus.OutForDelivery);
        Assert.Equal(DeliveryStatus.OutForDelivery, order.DeliveryStatus);

        order.UpdateDeliveryStatus(DeliveryStatus.Delivered);
        Assert.Equal(DeliveryStatus.Delivered, order.DeliveryStatus);
    }
}
