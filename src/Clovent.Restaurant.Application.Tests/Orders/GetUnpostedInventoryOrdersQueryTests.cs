using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Outbox;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Orders;

public sealed class GetUnpostedInventoryOrdersQueryTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyUnpostedInventoryOrders()
    {
        // Arrange
        var fakeRepo = new FakeOutboxRepository();
        var handler = new GetUnpostedInventoryOrdersQueryHandler(fakeRepo);

        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var completedOrderId = Guid.NewGuid();

        var correlationId = Guid.NewGuid().ToString();

        var pendingMsg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            orderId1.ToString(),
            correlationId,
            System.Text.Json.JsonSerializer.Serialize(new InventoryPostingPayload(
                orderId1,
                "ORD-101",
                Guid.NewGuid(),
                new List<InventoryPostingLineItem> { new(Guid.NewGuid(), "SKU1", "Item 1", 2m) })),
            $"inv-{orderId1}");

        var failedMsg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            orderId2.ToString(),
            correlationId,
            System.Text.Json.JsonSerializer.Serialize(new InventoryPostingPayload(
                orderId2,
                "ORD-102",
                Guid.NewGuid(),
                new List<InventoryPostingLineItem> { new(Guid.NewGuid(), "SKU2", "Item 2", 1m) })),
            $"inv-{orderId2}");
        failedMsg.ClaimForProcessing();
        failedMsg.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(1), "Inventory service offline");

        var completedMsg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            completedOrderId.ToString(),
            correlationId,
            System.Text.Json.JsonSerializer.Serialize(new InventoryPostingPayload(
                completedOrderId,
                "ORD-103",
                Guid.NewGuid(),
                new List<InventoryPostingLineItem> { new(Guid.NewGuid(), "SKU3", "Item 3", 5m) })),
            $"inv-{completedOrderId}");
        completedMsg.ClaimForProcessing();
        completedMsg.MarkCompleted(DateTimeOffset.UtcNow);

        // Also add non-inventory message (e.g. receipt print)
        var receiptMsg = OutboxMessage.Create(
            OutboxMessageType.ReceiptPrint,
            "Order",
            Guid.NewGuid().ToString(),
            "{}",
            Guid.NewGuid().ToString(),
            "rcpt-1");

        await fakeRepo.AddRangeAsync([pendingMsg, failedMsg, completedMsg, receiptMsg]);

        // Act
        var result = await handler.Handle(new GetUnpostedInventoryOrdersQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.OrderId == orderId1 && r.OrderNumber == "ORD-101" && r.OutboxStatus == "Pending");
        Assert.Contains(result, r => r.OrderId == orderId2 && r.OrderNumber == "ORD-102" && r.LastError == "Inventory service offline");
        Assert.DoesNotContain(result, r => r.OrderId == completedOrderId);
    }
}
