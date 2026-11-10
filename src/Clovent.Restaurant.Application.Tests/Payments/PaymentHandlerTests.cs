using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Payments.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Payments;

public class PaymentHandlerTests
{
    /// <summary>
    /// Builds a <see cref="RecordPaymentCommandHandler"/> and gives <paramref name="order"/> a
    /// single line worth <paramref name="billTotal"/>, so the handler's server-side
    /// outstanding-balance ceiling has a real bill to measure a payment against - an order with no
    /// lines has a grand total of zero, against which every payment is by definition an
    /// over-payment.
    /// </summary>
    private static RecordPaymentCommandHandler CreateRecordHandler(
        FakeOrderRepository orderRepository,
        FakePaymentRepository paymentRepository,
        FakeCustomerRepository customerRepository,
        FakeCustomerLedgerEntryRepository ledgerRepository,
        FakePaymentMethodRepository paymentMethodRepository,
        Order order,
        decimal billTotal)
    {
        var orderLineRepository = new FakeOrderLineRepository();
        var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, billTotal, 0, false);
        order.AddOrderLine(line.Id);
        orderLineRepository.Add(line);

        return new RecordPaymentCommandHandler(
            orderRepository,
            paymentRepository,
            customerRepository,
            ledgerRepository,
            paymentMethodRepository,
            orderLineRepository,
            new FakeDiscountRepository(),
            new FakeServiceChargeRepository(),
            new FakeCustomerPaymentAllocationRepository());
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_Valid_RecordsAgainstOrder()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        
        var paymentMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(paymentMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 25m);
        var result = await handler.Handle(new RecordPaymentCommand(order.Id.Value, paymentMethod.Id.Value, 25m), CancellationToken.None);

        Assert.Contains(new PaymentId(result.PaymentId), order.PaymentIds);
        Assert.Equal(25m, result.Amount);
    }

    [Fact]
    public async Task VoidPaymentCommandHandler_Valid_Voids()
    {
        var paymentMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        
        var repository = new FakePaymentRepository();
        var payment = Payment.Create(OrderId.New(), paymentMethod.Id, 25m);
        repository.Add(payment);

        var orderRepository = new FakeOrderRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        paymentMethodRepository.Add(paymentMethod);

        var result = await new VoidPaymentCommandHandler(repository, orderRepository, customerRepository, ledgerRepository, paymentMethodRepository, new FakeCustomerPaymentAllocationRepository()).Handle(new VoidPaymentCommand(payment.Id.Value), CancellationToken.None);

        Assert.True(result.IsVoided);
    }

    [Fact]
    public async Task ListPaymentsByOrderQueryHandler_FiltersToOrder()
    {
        var repository = new FakePaymentRepository();
        var orderId = OrderId.New();
        repository.Add(Payment.Create(orderId, PaymentMethodId.New(), 10m));
        repository.Add(Payment.Create(OrderId.New(), PaymentMethodId.New(), 20m));

        var result = await new ListPaymentsByOrderQueryHandler(repository).Handle(new ListPaymentsByOrderQuery(orderId.Value), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetPaymentByIdQueryHandler_NotFound_Throws()
    {
        var handler = new GetPaymentByIdQueryHandler(new FakePaymentRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetPaymentByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_CreditSale_UpdatesCustomerBalanceAndLedger()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "John Doe",
            "123456",
            "Address",
            "john@example.com",
            0m,
            1000m,
            null);
        customerRepository.Add(customer);

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 250m);
        var result = await handler.Handle(new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 250m), CancellationToken.None);

        Assert.Equal(250m, customer.OutstandingBalance);

        var ledgerEntries = await ledgerRepository.GetByCustomerIdAsync(customer.Id, CancellationToken.None);
        var entry = Assert.Single(ledgerEntries);
        Assert.Equal(250m, entry.Debit);
        Assert.Equal(0m, entry.Credit);
        Assert.Equal(250m, entry.RunningBalance);
        Assert.Equal("Credit Sale", entry.Description);
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_WalkInCreditRejected_Throws()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        // Walk-in customer: order.CustomerId remains null
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 50m);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 50m), CancellationToken.None));

        Assert.Equal("A customer must be selected for Credit / Pay Later sales.", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_CreditSale_InactiveCustomer_Throws()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "John Doe",
            "123456",
            "Address",
            "john@example.com",
            0m,
            1000m,
            null);
        customer.SetStatus(false); // Inactive
        customerRepository.Add(customer);

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 50m);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 50m), CancellationToken.None));

        Assert.Equal($"The customer '{customer.Name}' is inactive.", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_CreditLimitExceeded_NoOverride_Throws()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "John Doe",
            "123456",
            "Address",
            "john@example.com",
            0m,
            500m, // limit is 500
            null);
        customer.AdjustBalance(400m); // outstanding is 400
        customerRepository.Add(customer);

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 150m);

        // Adding 150 credit payment brings outstanding to 550, which exceeds 500 limit.
        var cmd = new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 150m, ExceedCreditLimitApproved: false);
        
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("Credit limit exceeded.", ex.Message);
        
        // Outstanding balance must remain unchanged
        Assert.Equal(400m, customer.OutstandingBalance);
        
        // Ledger entry must NOT have been written
        var ledgerEntries = await ledgerRepository.GetByCustomerIdAsync(customer.Id, CancellationToken.None);
        Assert.Empty(ledgerEntries);
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_CreditLimitExceeded_WithOverride_Succeeds()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "John Doe",
            "123456",
            "Address",
            "john@example.com",
            0m,
            500m, // limit is 500
            null);
        customer.AdjustBalance(400m); // outstanding is 400
        customerRepository.Add(customer);

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 150m);

        // Adding 150 credit payment brings outstanding to 550, but we pass ExceedCreditLimitApproved = true
        var cmd = new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 150m, ExceedCreditLimitApproved: true);
        
        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        
        // Outstanding balance must update correctly
        Assert.Equal(550m, customer.OutstandingBalance);
        
        // Ledger entry must have been written
        var ledgerEntries = await ledgerRepository.GetByCustomerIdAsync(customer.Id, CancellationToken.None);
        var entry = Assert.Single(ledgerEntries);
        Assert.Equal(150m, entry.Debit);
        Assert.Equal(0m, entry.Credit);
        Assert.Equal(550m, entry.RunningBalance);
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_CreditLimitExactlyReached_Succeeds()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "John Doe",
            "123456",
            "Address",
            "john@example.com",
            0m,
            500m, // limit is 500
            null);
        customer.AdjustBalance(400m); // outstanding is 400
        customerRepository.Add(customer);

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 100m);

        // Adding 100 credit payment brings outstanding to 500 (exactly limit).
        var cmd = new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 100m, ExceedCreditLimitApproved: false);
        
        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(500m, customer.OutstandingBalance);
    }

    [Fact]
    public async Task VoidPaymentCommandHandler_CreditSale_AdjustsBalanceAndLedger()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "John Doe",
            "123456",
            "Address",
            "john@example.com",
            0m,
            1000m,
            null);
        customerRepository.Add(customer);

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        // Record a credit sale payment of 150m first
        var recordHandler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 150m);
        var recordResult = await recordHandler.Handle(new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 150m), CancellationToken.None);
        
        Assert.Equal(150m, customer.OutstandingBalance);

        // Now void the payment
        var voidHandler = new VoidPaymentCommandHandler(paymentRepository, orderRepository, customerRepository, ledgerRepository, paymentMethodRepository, new FakeCustomerPaymentAllocationRepository());
        var voidResult = await voidHandler.Handle(new VoidPaymentCommand(recordResult.PaymentId), CancellationToken.None);

        Assert.True(voidResult.IsVoided);

        // Customer outstanding balance should go back to 0
        Assert.Equal(0m, customer.OutstandingBalance);

        // Verify the void ledger entry
        var ledgerEntries = await ledgerRepository.GetByCustomerIdAsync(customer.Id, CancellationToken.None);
        
        // Should have 2 entries: 1 for the original Credit Sale, 1 for the Void Credit Sale
        Assert.Equal(2, ledgerEntries.Count);
        
        var voidEntry = ledgerEntries.Last();
        Assert.Equal($"VOID-{recordResult.PaymentId}", voidEntry.Reference);
        Assert.Equal($"Void Credit Sale ({order.OrderNumber.Value})", voidEntry.Description);
        Assert.Equal(0m, voidEntry.Debit);
        Assert.Equal(150m, voidEntry.Credit);
        Assert.Equal(0m, voidEntry.RunningBalance);
    }

    [Fact]
    public async Task VoidPaymentCommandHandler_OnAccountPayment_AdjustsBalanceAndLedger()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C002"),
            "On Account Customer",
            "12345678",
            "Address 2",
            "onaccount@example.com",
            0m,
            1000m,
            null);
        customerRepository.Add(customer);

        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        paymentMethodRepository.Add(onAccountMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        // Record an On Account payment of 200m
        var recordHandler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 200m);
        var recordResult = await recordHandler.Handle(new RecordPaymentCommand(order.Id.Value, onAccountMethod.Id.Value, 200m), CancellationToken.None);

        Assert.Equal(200m, customer.OutstandingBalance);

        // Now void the On Account payment
        var voidHandler = new VoidPaymentCommandHandler(paymentRepository, orderRepository, customerRepository, ledgerRepository, paymentMethodRepository, new FakeCustomerPaymentAllocationRepository());
        var voidResult = await voidHandler.Handle(new VoidPaymentCommand(recordResult.PaymentId), CancellationToken.None);

        Assert.True(voidResult.IsVoided);

        // Customer outstanding balance should go back to 0
        Assert.Equal(0m, customer.OutstandingBalance);

        // Verify the void ledger entry
        var ledgerEntries = await ledgerRepository.GetByCustomerIdAsync(customer.Id, CancellationToken.None);
        Assert.Equal(2, ledgerEntries.Count);

        var voidEntry = ledgerEntries.Last();
        Assert.Equal($"VOID-{recordResult.PaymentId}", voidEntry.Reference);
        Assert.Equal(0m, voidEntry.Debit);
        Assert.Equal(200m, voidEntry.Credit);
        Assert.Equal(0m, voidEntry.RunningBalance);
    }

    [Fact]
    public async Task VoidPaymentCommandHandler_CustomerAdvancePayment_AdjustsBalanceAndLedger()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C003"),
            "Advance Customer",
            "98765432",
            "Address 3",
            "advance@example.com",
            0m,
            5000m,
            null);
        // Customer has Rs. 500 advance credit (negative outstanding balance)
        customer.AdjustBalance(-500m);
        customerRepository.Add(customer);

        var advanceMethod = PaymentMethod.Create(PaymentMethodName.Create("Customer Advance"));
        paymentMethodRepository.Add(advanceMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        // Record a Customer Advance payment of 200m
        var recordHandler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 200m);
        var recordResult = await recordHandler.Handle(new RecordPaymentCommand(order.Id.Value, advanceMethod.Id.Value, 200m), CancellationToken.None);

        // Outstanding balance changed from -500 to -300 (remaining advance 300)
        Assert.Equal(-300m, customer.OutstandingBalance);

        // Now void the Customer Advance payment
        var voidHandler = new VoidPaymentCommandHandler(paymentRepository, orderRepository, customerRepository, ledgerRepository, paymentMethodRepository, new FakeCustomerPaymentAllocationRepository());
        var voidResult = await voidHandler.Handle(new VoidPaymentCommand(recordResult.PaymentId), CancellationToken.None);

        Assert.True(voidResult.IsVoided);

        // Customer outstanding balance should be restored to -500 (full 500 advance restored)
        Assert.Equal(-500m, customer.OutstandingBalance);

        // Verify the void ledger entry
        var ledgerEntries = await ledgerRepository.GetByCustomerIdAsync(customer.Id, CancellationToken.None);
        Assert.Equal(2, ledgerEntries.Count);

        var voidEntry = ledgerEntries.Last();
        Assert.Equal($"VOID-{recordResult.PaymentId}", voidEntry.Reference);
        Assert.Equal(0m, voidEntry.Debit);
        Assert.Equal(200m, voidEntry.Credit);
        Assert.Equal(-500m, voidEntry.RunningBalance);
    }

    [Fact]
    public async Task VoidPaymentCommandHandler_OnAccountWithAdvanceApplied_CleansUpAdvanceAllocation()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var allocationRepository = new FakeCustomerPaymentAllocationRepository();
        var orderLineRepository = new FakeOrderLineRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C005"),
            "Advance-Backed OnAccount",
            "5550005",
            "Address 5",
            "adv-onacct@example.com",
            0m,
            1000m,
            null);
        customer.AdjustBalance(-300m); // 300 advance
        customerRepository.Add(customer);

        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        paymentMethodRepository.Add(onAccountMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 500m, 0, false);
        order.AddOrderLine(line.Id);
        orderLineRepository.Add(line);
        orderRepository.Add(order);

        var recordHandler = new RecordPaymentCommandHandler(
            orderRepository,
            paymentRepository,
            customerRepository,
            ledgerRepository,
            paymentMethodRepository,
            orderLineRepository,
            new FakeDiscountRepository(),
            new FakeServiceChargeRepository(),
            allocationRepository);

        var recResult = await recordHandler.Handle(new RecordPaymentCommand(order.Id.Value, onAccountMethod.Id.Value, 500m), CancellationToken.None);

        // Balance went -300 -> +200; 300 advance allocation created on order
        Assert.Equal(200m, customer.OutstandingBalance);
        Assert.Single(await allocationRepository.GetByOrderIdAsync(order.Id));

        var voidHandler = new VoidPaymentCommandHandler(
            paymentRepository,
            orderRepository,
            customerRepository,
            ledgerRepository,
            paymentMethodRepository,
            allocationRepository);

        await voidHandler.Handle(new VoidPaymentCommand(recResult.PaymentId), CancellationToken.None);

        // Balance restored to -300 and the 300 advance allocation MUST be removed
        Assert.Equal(-300m, customer.OutstandingBalance);
        Assert.Empty(await allocationRepository.GetByOrderIdAsync(order.Id));
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_HeldOrder_ThrowsBeforeMutatingCustomerBalanceOrLedger()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var allocationRepository = new FakeCustomerPaymentAllocationRepository();
        var orderLineRepository = new FakeOrderLineRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C006"),
            "Held Order Customer",
            "5550006",
            "Address 6",
            "held@example.com",
            0m,
            1000m,
            null);
        customer.AdjustBalance(-400m); // 400 advance
        customerRepository.Add(customer);

        var advanceMethod = PaymentMethod.Create(PaymentMethodName.Create("Customer Advance"));
        paymentMethodRepository.Add(advanceMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 200m, 0, false);
        order.AddOrderLine(line.Id);
        order.Hold(); // Not Open!
        orderLineRepository.Add(line);
        orderRepository.Add(order);

        var recordHandler = new RecordPaymentCommandHandler(
            orderRepository,
            paymentRepository,
            customerRepository,
            ledgerRepository,
            paymentMethodRepository,
            orderLineRepository,
            new FakeDiscountRepository(),
            new FakeServiceChargeRepository(),
            allocationRepository);

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            recordHandler.Handle(new RecordPaymentCommand(order.Id.Value, advanceMethod.Id.Value, 200m), CancellationToken.None));

        // Failure MUST NOT leak balance, ledger, or allocation mutations
        Assert.Equal(-400m, customerRepository.GetPersistedBalance(customer.Id));
        Assert.Empty(await ledgerRepository.GetByCustomerIdAsync(customer.Id));
        Assert.Empty(await allocationRepository.GetByOrderIdAsync(order.Id));
    }

    [Fact]
    public async Task VoidPaymentCommandHandler_RepeatedAndConcurrentVoid_ReversesBalanceExactlyOnce()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var allocationRepository = new FakeCustomerPaymentAllocationRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C007"),
            "Concurrent Void Customer",
            "5550007",
            "Address 7",
            "conc@example.com",
            0m,
            2000m,
            null);
        customerRepository.Add(customer);

        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        paymentMethodRepository.Add(onAccountMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var recordHandler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 350m);
        var recResult = await recordHandler.Handle(new RecordPaymentCommand(order.Id.Value, onAccountMethod.Id.Value, 350m), CancellationToken.None);
        Assert.Equal(350m, customer.OutstandingBalance);

        var voidHandler = new VoidPaymentCommandHandler(paymentRepository, orderRepository, customerRepository, ledgerRepository, paymentMethodRepository, allocationRepository);

        // Concurrent submission of VoidPaymentCommand on the same payment
        var tasks = new[]
        {
            Task.Run(() => voidHandler.Handle(new VoidPaymentCommand(recResult.PaymentId), CancellationToken.None)),
            Task.Run(() => voidHandler.Handle(new VoidPaymentCommand(recResult.PaymentId), CancellationToken.None))
        };

        int succeeded = 0;
        int rejected = 0;
        foreach (var t in tasks)
        {
            try
            {
                await t;
                succeeded++;
            }
            catch (InvalidOperationException)
            {
                rejected++;
            }
        }

        Assert.Equal(1, succeeded);
        Assert.Equal(1, rejected);

        // Repeated sequential void must also reject
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            voidHandler.Handle(new VoidPaymentCommand(recResult.PaymentId), CancellationToken.None));

        // Balance reversed strictly ONCE
        Assert.Equal(0m, customerRepository.GetPersistedBalance(customer.Id));
        var ledgerEntries = await ledgerRepository.GetByCustomerIdAsync(customer.Id);
        Assert.Equal(2, ledgerEntries.Count); // 1 debit + 1 void credit
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_ConcurrentSplitPayments_DoesNotOverpayOrder()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var allocationRepository = new FakeCustomerPaymentAllocationRepository();

        var cash = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var card = PaymentMethod.Create(PaymentMethodName.Create("Card"));
        paymentMethodRepository.Add(cash);
        paymentMethodRepository.Add(card);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 1000m, 0, false);
        order.AddOrderLine(line.Id);
        orderLineRepository.Add(line);
        orderRepository.Add(order);

        var handler = new RecordPaymentCommandHandler(
            orderRepository,
            paymentRepository,
            customerRepository,
            ledgerRepository,
            paymentMethodRepository,
            orderLineRepository,
            new FakeDiscountRepository(),
            new FakeServiceChargeRepository(),
            allocationRepository);

        var t1 = Task.Run(() => handler.Handle(new RecordPaymentCommand(order.Id.Value, cash.Id.Value, 600m), CancellationToken.None));
        var t2 = Task.Run(() => handler.Handle(new RecordPaymentCommand(order.Id.Value, card.Id.Value, 600m), CancellationToken.None));

        int succeeded = 0;
        int rejected = 0;
        foreach (var t in new[] { t1, t2 })
        {
            try
            {
                await t;
                succeeded++;
            }
            catch (InvalidOperationException)
            {
                rejected++;
            }
        }

        Assert.Equal(1, succeeded);
        Assert.Equal(1, rejected);

        var payments = await paymentRepository.GetByOrderIdAsync(order.Id);
        Assert.Single(payments);
        Assert.Equal(600m, payments.Sum(p => p.Amount));
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_UnauthorizedManagerOverride_ThrowsAndLogsNothing()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var activityLogRepository = new FakeActivityLogEntryRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "John Doe",
            "123456",
            "Address",
            "john@example.com",
            0m,
            500m, // limit is 500
            null);
        customer.AdjustBalance(400m); // outstanding is 400
        customerRepository.Add(customer);

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 150m);

        // ExceedCreditLimitApproved = false simulates user who does NOT have pos.exceedcreditlimit
        var cmd = new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 150m, ExceedCreditLimitApproved: false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("Credit limit exceeded.", ex.Message);

        // Verify customer outstanding balance is unchanged
        Assert.Equal(400m, customer.OutstandingBalance);

        // Verify no credit-sale ledger entry is created
        var ledgerEntries = await ledgerRepository.GetByCustomerIdAsync(customer.Id, CancellationToken.None);
        Assert.Empty(ledgerEntries);

        // Verify no successful override activity log is created
        var activityLogs = activityLogRepository.GetAll();
        Assert.Empty(activityLogs);
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_AuthorizedOverrideAuditLogging_SucceedsAndLogs()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var activityLogRepository = new FakeActivityLogEntryRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "John Doe",
            "123456",
            "Address",
            "john@example.com",
            0m,
            500m, // limit is 500
            null);
        customer.AdjustBalance(400m); // outstanding is 400
        customerRepository.Add(customer);

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        // Execute as a user who HAS pos.exceedcreditlimit and approves manager override:
        // 1. Log the activity record (Override)
        var activityHandler = new RecordActivityCommandHandler(activityLogRepository);
        var logCmd = new RecordActivityCommand("Override", $"Authorized credit limit override for customer '{customer.Name}' ({customer.Code})", "ManagerUser", "Workstation01");
        await activityHandler.Handle(logCmd, CancellationToken.None);

        // 2. Record payment with ExceedCreditLimitApproved = true
        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 150m);
        var cmd = new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 150m, ExceedCreditLimitApproved: true);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);

        // Verify customer balance and ledger are updated exactly once
        Assert.Equal(550m, customer.OutstandingBalance);
        var ledgerEntries = await ledgerRepository.GetByCustomerIdAsync(customer.Id, CancellationToken.None);
        var ledgerEntry = Assert.Single(ledgerEntries); // Exactly one ledger entry
        Assert.Equal(150m, ledgerEntry.Debit);
        Assert.Equal(550m, ledgerEntry.RunningBalance);

        // Verify the expected override/activity-log record is created with correct success/approval details
        var activityLogs = activityLogRepository.GetAll();
        var log = Assert.Single(activityLogs);
        Assert.Equal("Override", log.Action);
        Assert.Contains("Authorized credit limit override for customer 'John Doe'", log.Details);
        Assert.Equal("ManagerUser", log.PerformedBy);
        Assert.Equal("Workstation01", log.MachineName);
    }

    [Fact]
    public async Task RecordPaymentCommandHandler_DeniedOverrideAuditSafety_FailsAndDoesNotLog()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var activityLogRepository = new FakeActivityLogEntryRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "John Doe",
            "123456",
            "Address",
            "john@example.com",
            0m,
            500m, // limit is 500
            null);
        customer.AdjustBalance(400m); // outstanding is 400
        customerRepository.Add(customer);

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 150m);

        // ExceedCreditLimitApproved = false simulates manager override is denied/cancelled
        var cmd = new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 150m, ExceedCreditLimitApproved: false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("Credit limit exceeded.", ex.Message);

        // Verify credit sale does not proceed (no payment recorded)
        var payments = await paymentRepository.GetByOrderIdAsync(order.Id, CancellationToken.None);
        Assert.Empty(payments);

        // Verify customer balance remains unchanged
        Assert.Equal(400m, customer.OutstandingBalance);

        // Verify no successful override audit entry is created
        var activityLogs = activityLogRepository.GetAll();
        Assert.Empty(activityLogs);
    }

    /// <summary>
    /// Partial payments must keep working: each one only shrinks the balance the next is measured
    /// against, so the server-side ceiling must not mistake a second legitimate instalment for an
    /// over-payment.
    /// </summary>
    [Fact]
    public async Task RecordPaymentCommandHandler_PartialPayments_BothSucceed()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var cash = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cash);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 500m);

        var first = await handler.Handle(new RecordPaymentCommand(order.Id.Value, cash.Id.Value, 200m), CancellationToken.None);
        var second = await handler.Handle(new RecordPaymentCommand(order.Id.Value, cash.Id.Value, 300m), CancellationToken.None);

        Assert.Equal(200m, first.Amount);
        Assert.Equal(300m, second.Amount);

        var payments = await paymentRepository.GetByOrderIdAsync(order.Id, CancellationToken.None);
        Assert.Equal(2, payments.Count);
        Assert.Equal(500m, payments.Sum(p => p.Amount));
    }

    /// <summary>
    /// The duplicate-click case: a second full-balance payment against an already-settled bill must
    /// be refused by the server, not silently recorded as a second row (a $500 bill paid twice as
    /// $1,000).
    /// </summary>
    [Fact]
    public async Task RecordPaymentCommandHandler_SecondFullPaymentOnSettledBill_Throws()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var cash = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cash);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 500m);

        await handler.Handle(new RecordPaymentCommand(order.Id.Value, cash.Id.Value, 500m), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RecordPaymentCommand(order.Id.Value, cash.Id.Value, 500m), CancellationToken.None));

        Assert.Contains("more than the 0.00 still outstanding", ex.Message);

        // The bill must still show exactly one payment of 500.
        var payments = await paymentRepository.GetByOrderIdAsync(order.Id, CancellationToken.None);
        Assert.Equal(500m, Assert.Single(payments).Amount);
    }

    /// <summary>A payment larger than what is left - 200 against a remaining 100 - must be refused.</summary>
    [Fact]
    public async Task RecordPaymentCommandHandler_PaymentExceedsRemainingBalance_Throws()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var cash = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cash);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 500m);

        await handler.Handle(new RecordPaymentCommand(order.Id.Value, cash.Id.Value, 400m), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RecordPaymentCommand(order.Id.Value, cash.Id.Value, 200m), CancellationToken.None));

        Assert.Contains("more than the 100.00 still outstanding", ex.Message);

        // Settling the actual remaining 100 must still be accepted.
        var final = await handler.Handle(new RecordPaymentCommand(order.Id.Value, cash.Id.Value, 100m), CancellationToken.None);
        Assert.Equal(100m, final.Amount);
    }

    /// <summary>
    /// The correction path the Order History screen exists to enable: a surplus
    /// payment on an already-<b>Completed</b> order is voided, and the order is
    /// left exactly as it was. This is the ORD-35 shape - 4 x 165.00 against a
    /// 280.00 bill - and the cleanup must never reopen or re-complete the
    /// order, because re-completing it would issue its stock a second time.
    /// </summary>
    [Fact]
    public async Task VoidPaymentCommandHandler_OnCompletedOrder_ThrowsAndPreservesImmutability()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var allocationRepository = new FakeCustomerPaymentAllocationRepository();

        var method = PaymentMethod.Create(PaymentMethodName.Create("Easy Paisa"));
        paymentMethodRepository.Add(method);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 280m, 0, false);
        order.AddOrderLine(line.Id);

        var payment = Payment.Create(order.Id, method.Id, 280m);
        order.RecordPayment(payment.Id);
        paymentRepository.Add(payment);

        order.Complete();
        orderRepository.Add(order);

        var handler = new VoidPaymentCommandHandler(paymentRepository, orderRepository, customerRepository, ledgerRepository, paymentMethodRepository, allocationRepository);

        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new VoidPaymentCommand(payment.Id.Value), CancellationToken.None));

        Assert.Contains("Completed transactions are immutable", ex.Message);
        Assert.False(payment.IsVoided);
        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    /// <summary>
    /// The H-4 shape - ORD-2/ORD-4, cancelled while still holding money. The
    /// payment is voided and the order stays <b>Cancelled</b>: it is never
    /// reopened and never re-cancelled.
    /// </summary>
    [Fact]
    public async Task VoidPaymentCommandHandler_OnCancelledOrder_VoidsPaymentAndOrderStaysCancelled()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var method = PaymentMethod.Create(PaymentMethodName.Create("Credit Card"));
        paymentMethodRepository.Add(method);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.Cancel("Started by mistake");
        orderRepository.Add(order);

        // The payment predates the guard that now refuses this state, so it is
        // attached directly rather than through RecordPaymentCommand.
        var payment = Payment.Create(order.Id, method.Id, 57.50m);
        paymentRepository.Add(payment);

        var handler = new VoidPaymentCommandHandler(paymentRepository, orderRepository, customerRepository, ledgerRepository, paymentMethodRepository, new FakeCustomerPaymentAllocationRepository());

        var result = await handler.Handle(new VoidPaymentCommand(payment.Id.Value), CancellationToken.None);

        Assert.True(result.IsVoided);
        Assert.Equal("Cancelled", order.Status.ToString());

        var payments = await paymentRepository.GetByOrderIdAsync(order.Id, CancellationToken.None);
        Assert.Equal(0m, payments.Where(p => !p.IsVoided).Sum(p => p.Amount));
    }

    /// <summary>A non-credit payment must not touch any customer ledger - ORD-35's Easy Paisa and ORD-2/ORD-4's Credit Card payments all take this path.</summary>
    [Fact]
    public async Task VoidPaymentCommandHandler_NonCreditMethod_LeavesCustomerLedgerAlone()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "John Doe", "123456", "Address", "john@example.com", 0m, 1000m, null);
        customerRepository.Add(customer);

        var method = PaymentMethod.Create(PaymentMethodName.Create("Credit Card"));
        paymentMethodRepository.Add(method);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var payment = Payment.Create(order.Id, method.Id, 100m);
        paymentRepository.Add(payment);

        var handler = new VoidPaymentCommandHandler(paymentRepository, orderRepository, customerRepository, ledgerRepository, paymentMethodRepository, new FakeCustomerPaymentAllocationRepository());
        await handler.Handle(new VoidPaymentCommand(payment.Id.Value), CancellationToken.None);

        Assert.Equal(0m, customer.OutstandingBalance);
        Assert.Empty(await ledgerRepository.GetByCustomerIdAsync(customer.Id, CancellationToken.None));
    }

    #region Payment Record Regression Suite (PAYMENT-RECORD-01 to 12)

    [Fact]
    public async Task PAYMENT_RECORD_01_ExactCashPayment_RecordsPaymentSuccessfully()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cashMethod);

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), Clovent.Restaurant.Tables.TableId.New());
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 630.00m);
        var result = await handler.Handle(new RecordPaymentCommand(order.Id.Value, cashMethod.Id.Value, 630.00m), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(630.00m, result.Amount);
        Assert.Contains(new PaymentId(result.PaymentId), order.PaymentIds);
    }

    [Fact]
    public async Task PAYMENT_RECORD_02_PaymentWithShiftId_AssociatesShiftWithPayment()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cashMethod);

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), Clovent.Restaurant.Tables.TableId.New());
        orderRepository.Add(order);

        var shiftId = Guid.NewGuid();
        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 500.00m);
        var result = await handler.Handle(new RecordPaymentCommand(order.Id.Value, cashMethod.Id.Value, 500.00m, ShiftId: shiftId), CancellationToken.None);

        Assert.Equal(shiftId, result.ShiftId);
        var payment = await paymentRepository.GetByIdAsync(new PaymentId(result.PaymentId), CancellationToken.None);
        Assert.NotNull(payment);
        Assert.Equal(shiftId, payment.ShiftId?.Value);
    }

    [Fact]
    public async Task PAYMENT_RECORD_03_PartialPayment_ReducesBalanceSuccessfully()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cashMethod);

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), Clovent.Restaurant.Tables.TableId.New());
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 630.00m);
        var result = await handler.Handle(new RecordPaymentCommand(order.Id.Value, cashMethod.Id.Value, 300.00m), CancellationToken.None);

        Assert.Equal(300.00m, result.Amount);
    }

    [Fact]
    public async Task PAYMENT_RECORD_04_OverPayment_ThrowsInvalidOperationException()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cashMethod);

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), Clovent.Restaurant.Tables.TableId.New());
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 100.00m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RecordPaymentCommand(order.Id.Value, cashMethod.Id.Value, 150.00m), CancellationToken.None));
    }

    [Fact]
    public async Task PAYMENT_RECORD_05_SplitPaymentAcrossTwoMethods_Succeeds()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var cardMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit Card"));
        paymentMethodRepository.Add(cashMethod);
        paymentMethodRepository.Add(cardMethod);

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), Clovent.Restaurant.Tables.TableId.New());
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 630.00m);

        var pay1 = await handler.Handle(new RecordPaymentCommand(order.Id.Value, cashMethod.Id.Value, 300.00m), CancellationToken.None);
        var pay2 = await handler.Handle(new RecordPaymentCommand(order.Id.Value, cardMethod.Id.Value, 330.00m), CancellationToken.None);

        Assert.Equal(300.00m, pay1.Amount);
        Assert.Equal(330.00m, pay2.Amount);
        Assert.Equal(2, order.PaymentIds.Count);
    }

    [Fact]
    public async Task PAYMENT_RECORD_06_CreditPaymentWithoutCustomer_ThrowsInvalidOperationException()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 100.00m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 100.00m), CancellationToken.None));
    }

    [Fact]
    public async Task PAYMENT_RECORD_07_CreditPaymentExceedingLimitWithoutApproval_ThrowsInvalidOperationException()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "Customer A", "123456", "Address", "a@test.com", 0m, 500m, null);
        customerRepository.Add(customer);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 600.00m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 600.00m, ExceedCreditLimitApproved: false), CancellationToken.None));
    }

    [Fact]
    public async Task PAYMENT_RECORD_08_CreditPaymentExceedingLimitWithApproval_Succeeds()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "Customer A", "123456", "Address", "a@test.com", 0m, 500m, null);
        customerRepository.Add(customer);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 600.00m);
        var result = await handler.Handle(new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 600.00m, ExceedCreditLimitApproved: true), CancellationToken.None);

        Assert.Equal(600.00m, result.Amount);
        Assert.Equal(600.00m, customer.OutstandingBalance);
    }

    [Fact]
    public async Task PAYMENT_RECORD_09_NonExistentOrder_ThrowsNotFoundException()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cashMethod);

        var handler = new RecordPaymentCommandHandler(
            orderRepository, paymentRepository, customerRepository, ledgerRepository,
            paymentMethodRepository, new FakeOrderLineRepository(), new FakeDiscountRepository(), new FakeServiceChargeRepository(), new FakeCustomerPaymentAllocationRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RecordPaymentCommand(Guid.NewGuid(), cashMethod.Id.Value, 50.00m), CancellationToken.None));
    }

    [Fact]
    public async Task PAYMENT_RECORD_10_NonExistentPaymentMethod_ThrowsNotFoundException()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = new RecordPaymentCommandHandler(
            orderRepository, paymentRepository, customerRepository, ledgerRepository,
            paymentMethodRepository, new FakeOrderLineRepository(), new FakeDiscountRepository(), new FakeServiceChargeRepository(), new FakeCustomerPaymentAllocationRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RecordPaymentCommand(order.Id.Value, Guid.NewGuid(), 50.00m), CancellationToken.None));
    }

    [Fact]
    public async Task PAYMENT_RECORD_11_InactiveCustomerOnCreditSale_ThrowsInvalidOperationException()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var creditMethod = PaymentMethod.Create(PaymentMethodName.Create("Credit"));
        paymentMethodRepository.Add(creditMethod);

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C001"),
            "Inactive Customer", "123456", "Address", "ina@test.com", 0m, 1000m, null);
        customer.SetStatus(false);
        customerRepository.Add(customer);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 100.00m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RecordPaymentCommand(order.Id.Value, creditMethod.Id.Value, 100.00m), CancellationToken.None));
    }

    [Fact]
    public async Task PAYMENT_RECORD_12_ZeroBalanceOrderPayment_ThrowsInvalidOperationException()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        paymentMethodRepository.Add(cashMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateRecordHandler(orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository, order, 100.00m);

        // First payment settles balance to 0
        await handler.Handle(new RecordPaymentCommand(order.Id.Value, cashMethod.Id.Value, 100.00m), CancellationToken.None);

        // Attempting another payment must throw InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RecordPaymentCommand(order.Id.Value, cashMethod.Id.Value, 10.00m), CancellationToken.None));
    }

    #endregion

    [Fact]
    public async Task VoidPaymentCommandHandler_AllocationRepositoryFailure_DoesNotMutateBalanceOrPaymentStatus()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var allocationRepository = new FakeCustomerPaymentAllocationRepository();
        var orderLineRepository = new FakeOrderLineRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C010"),
            "Partial Fail Customer",
            "5550010",
            "Address 10",
            "partial@example.com",
            0m,
            1000m,
            null);
        customer.AdjustBalance(-200m);
        customerRepository.Add(customer);

        var advanceMethod = PaymentMethod.Create(PaymentMethodName.Create("Customer Advance"));
        paymentMethodRepository.Add(advanceMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 200m, 0, false);
        order.AddOrderLine(line.Id);
        orderLineRepository.Add(line);
        orderRepository.Add(order);

        var recHandler = new RecordPaymentCommandHandler(
            orderRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository,
            orderLineRepository, new FakeDiscountRepository(), new FakeServiceChargeRepository(), allocationRepository);
        var payDto = await recHandler.Handle(new RecordPaymentCommand(order.Id.Value, advanceMethod.Id.Value, 200m), CancellationToken.None);
        Assert.Equal(0m, customerRepository.GetPersistedBalance(customer.Id));

        var throwingAllocRepo = new ThrowingAllocationRepository(new IOException("Simulated allocation repository failure"));
        var voidHandler = new VoidPaymentCommandHandler(
            paymentRepository, orderRepository, customerRepository, ledgerRepository, paymentMethodRepository, throwingAllocRepo);

        await Assert.ThrowsAsync<IOException>(() =>
            voidHandler.Handle(new VoidPaymentCommand(payDto.PaymentId), CancellationToken.None));

        // Failure in allocation lookup/cleanup MUST NOT mutate customer balance, ledger, or payment status
        Assert.Equal(0m, customerRepository.GetPersistedBalance(customer.Id));
        Assert.Single(await ledgerRepository.GetByCustomerIdAsync(customer.Id));
        var storedPayment = await paymentRepository.GetByIdAsync(new PaymentId(payDto.PaymentId));
        Assert.NotNull(storedPayment);
        Assert.False(storedPayment.IsVoided);
    }

    [Fact]
    public async Task VoidPaymentCommandHandler_TrueAsyncConcurrentVoids_WithYieldingRepo_ReversesBalanceExactlyOnce()
    {
        var orderRepository = new FakeOrderRepository();
        var backingPaymentRepo = new FakePaymentRepository();
        var yieldingPaymentRepo = new YieldingPaymentRepository(backingPaymentRepo);
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();
        var allocationRepository = new FakeCustomerPaymentAllocationRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("C011"),
            "Yielding Concurrent Customer",
            "5550011",
            "Address 11",
            "yield@example.com",
            0m,
            2000m,
            null);
        customerRepository.Add(customer);

        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        paymentMethodRepository.Add(onAccountMethod);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.SetCustomer(customer.Id);
        orderRepository.Add(order);

        var recHandler = CreateRecordHandler(orderRepository, backingPaymentRepo, customerRepository, ledgerRepository, paymentMethodRepository, order, 450m);
        var recResult = await recHandler.Handle(new RecordPaymentCommand(order.Id.Value, onAccountMethod.Id.Value, 450m), CancellationToken.None);
        Assert.Equal(450m, customerRepository.GetPersistedBalance(customer.Id));

        var voidHandler = new VoidPaymentCommandHandler(
            yieldingPaymentRepo, orderRepository, customerRepository, ledgerRepository, paymentMethodRepository, allocationRepository);

        var results = await Task.WhenAll(
            AttemptVoidAsync(voidHandler, recResult.PaymentId),
            AttemptVoidAsync(voidHandler, recResult.PaymentId));

        Assert.Equal(1, results.Count(r => r));
        Assert.Equal(1, results.Count(r => !r));
        Assert.Equal(0m, customerRepository.GetPersistedBalance(customer.Id));
        Assert.Equal(2, (await ledgerRepository.GetByCustomerIdAsync(customer.Id)).Count);
    }

    private static async Task<bool> AttemptVoidAsync(VoidPaymentCommandHandler handler, Guid paymentId)
    {
        try
        {
            await handler.Handle(new VoidPaymentCommand(paymentId), CancellationToken.None);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private sealed class ThrowingAllocationRepository(Exception ex) : ICustomerPaymentAllocationRepository
    {
        public Task AddAsync(CustomerPaymentAllocation allocation, CancellationToken cancellationToken = default) => Task.FromException(ex);
        public Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyCollection<CustomerPaymentAllocation>>(ex);
        public Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyCollection<CustomerPaymentAllocation>>(ex);
        public Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyCollection<CustomerPaymentAllocation>>(ex);
        public Task RemoveAsync(CustomerPaymentAllocation allocation, CancellationToken cancellationToken = default) => Task.FromException(ex);
    }

    private sealed class YieldingPaymentRepository(IPaymentRepository inner) : IPaymentRepository
    {
        public async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
        {
            await Task.Delay(15, cancellationToken);
            return await inner.GetByIdAsync(id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<Payment>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return await inner.GetByOrderIdAsync(orderId, cancellationToken);
        }

        public Task<IReadOnlyCollection<Payment>> GetByShiftIdAsync(Clovent.Restaurant.Shifts.ShiftId shiftId, CancellationToken cancellationToken = default) =>
            inner.GetByShiftIdAsync(shiftId, cancellationToken);

        public Task<IReadOnlyCollection<Payment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            inner.GetAllAsync(cancellationToken);

        public Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
            inner.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);

        public Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
            inner.AddAsync(payment, cancellationToken);
    }
}

