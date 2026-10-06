using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Payments;

public sealed class PaymentIdempotencyTests
{
    [Fact]
    public async Task RecordPayment_WithSameIdempotencyKey_ReturnsExistingPaymentWithoutDuplicates()
    {
        // Arrange
        var orderRepo = new FakeOrderRepository();
        var lineRepo = new FakeOrderLineRepository();
        var paymentRepo = new FakePaymentRepository();
        var methodRepo = new FakePaymentMethodRepository();
        var customerRepo = new FakeCustomerRepository();
        var ledgerRepo = new FakeCustomerLedgerEntryRepository();
        var discountRepo = new FakeDiscountRepository();
        var serviceRepo = new FakeServiceChargeRepository();

        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        await methodRepo.AddAsync(method);

        var order = Order.Create(
            OrderType.TakeAway,
            new WarehouseId(Guid.NewGuid()),
            null,
            OrderNumber.Generate(DateTimeOffset.UtcNow));
        await orderRepo.AddAsync(order);

        var line = OrderLine.Create(
            order.Id,
            new ProductVariantId(Guid.NewGuid()),
            1m,
            100m,
            0m,
            false);
        await lineRepo.AddAsync(line);
        order.AddOrderLine(line.Id);

        var handler = new RecordPaymentCommandHandler(
            orderRepo,
            paymentRepo,
            customerRepo,
            ledgerRepo,
            methodRepo,
            lineRepo,
            discountRepo,
            serviceRepo);

        var command = new RecordPaymentCommand(
            order.Id.Value,
            method.Id.Value,
            100m,
            IdempotencyKey: "req-key-abc-123");

        // Act 1: Initial payment
        var result1 = await handler.Handle(command, CancellationToken.None);

        // Act 2: Duplicate retry with identical idempotency key
        var result2 = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.PaymentId, result2.PaymentId);
        Assert.Equal(100m, result2.Amount);

        var allPayments = await paymentRepo.GetByOrderIdAsync(order.Id);
        Assert.Single(allPayments);
    }

    [Fact]
    public async Task RecordPayment_WithSameKey_DifferingPayload_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderRepo = new FakeOrderRepository();
        var lineRepo = new FakeOrderLineRepository();
        var paymentRepo = new FakePaymentRepository();
        var methodRepo = new FakePaymentMethodRepository();
        var customerRepo = new FakeCustomerRepository();
        var ledgerRepo = new FakeCustomerLedgerEntryRepository();
        var discountRepo = new FakeDiscountRepository();
        var serviceRepo = new FakeServiceChargeRepository();

        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        await methodRepo.AddAsync(method);

        var order = Order.Create(
            OrderType.TakeAway,
            new WarehouseId(Guid.NewGuid()),
            null,
            OrderNumber.Generate(DateTimeOffset.UtcNow));
        await orderRepo.AddAsync(order);

        var line = OrderLine.Create(
            order.Id,
            new ProductVariantId(Guid.NewGuid()),
            2m,
            100m,
            0m,
            false);
        await lineRepo.AddAsync(line);
        order.AddOrderLine(line.Id);

        var handler = new RecordPaymentCommandHandler(
            orderRepo,
            paymentRepo,
            customerRepo,
            ledgerRepo,
            methodRepo,
            lineRepo,
            discountRepo,
            serviceRepo);

        var command1 = new RecordPaymentCommand(
            order.Id.Value,
            method.Id.Value,
            100m,
            IdempotencyKey: "unique-payment-key-999");

        // Act 1: Initial payment succeeds
        await handler.Handle(command1, CancellationToken.None);

        // Act 2: Submit with same idempotency key but differing amount (150m instead of 100m)
        var command2 = new RecordPaymentCommand(
            order.Id.Value,
            method.Id.Value,
            150m,
            IdempotencyKey: "unique-payment-key-999");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command2, CancellationToken.None));
        Assert.Contains("differing payloads is prohibited", ex.Message);
    }
}
