using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Outbox.Handlers;
using Clovent.Restaurant.Application.QuickBooks;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.QuickBooks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.QuickBooks;

public sealed class QuickBooksSyncHandlerTests
{
    [Fact]
    public async Task InvoiceSyncHandler_SuccessfulSync_CreatesSynchronizedMap()
    {
        // Arrange
        var qbGateway = new DefaultQuickBooksGateway();
        var cbRegistry = new CircuitBreakerRegistry();
        var repo = new FakeQuickBooksSyncMapRepository();
        var handler = new QuickBooksInvoiceSyncHandler(
            qbGateway,
            cbRegistry,
            NullLogger<QuickBooksInvoiceSyncHandler>.Instance,
            repo);

        var orderId = Guid.NewGuid();
        var invoiceRequest = new QuickBooksInvoiceRequest(
            OrderId: orderId,
            OrderNumber: "ORD-9001",
            CustomerName: "John Doe",
            SubTotal: 100.00m,
            TaxAmount: 10.00m,
            DiscountAmount: 5.00m,
            TotalAmount: 105.00m,
            Currency: "USD",
            TxnDate: DateTimeOffset.UtcNow,
            Lines: [new QuickBooksInvoiceLineItem(Guid.NewGuid(), "BURG-01", "Burger", 2m, 50.00m, 100.00m)]);

        var message = OutboxMessage.Create(
            OutboxMessageType.QuickBooksInvoiceSync,
            "Order",
            orderId.ToString(),
            orderId.ToString(),
            JsonSerializer.Serialize(invoiceRequest));

        message.ClaimForProcessing();

        // Act
        await handler.HandleAsync(message, CancellationToken.None);

        // Assert
        var map = await repo.GetByLocalEntityAsync(orderId, QuickBooksSyncEntityType.Invoice);
        Assert.NotNull(map);
        Assert.Equal(QuickBooksSyncStatus.Synchronized, map.Status);
        Assert.NotNull(map.QuickBooksTxnId);
        Assert.StartsWith("QB-TXN-INV-", map.QuickBooksTxnId);
        Assert.Equal("ORD-9001", map.QuickBooksDocNumber);
        Assert.NotNull(map.SyncedAtUtc);
        Assert.Null(map.LastError);
    }

    [Fact]
    public async Task InvoiceSyncHandler_AlreadySynchronized_SkipsDuplicatePost()
    {
        // Arrange
        var qbGateway = new DefaultQuickBooksGateway();
        var cbRegistry = new CircuitBreakerRegistry();
        var repo = new FakeQuickBooksSyncMapRepository();
        var handler = new QuickBooksInvoiceSyncHandler(
            qbGateway,
            cbRegistry,
            NullLogger<QuickBooksInvoiceSyncHandler>.Instance,
            repo);

        var orderId = Guid.NewGuid();
        var existingMap = QuickBooksSyncMap.Create(
            localEntityId: orderId,
            entityType: QuickBooksSyncEntityType.Invoice,
            amount: 75.00m,
            currency: "USD");

        existingMap.MarkSynchronized("QB-TXN-EXISTING", "ORD-8888");
        await repo.AddAsync(existingMap);

        var invoiceRequest = new QuickBooksInvoiceRequest(
            OrderId: orderId,
            OrderNumber: "ORD-8888",
            CustomerName: "Jane Smith",
            SubTotal: 75.00m,
            TaxAmount: 0m,
            DiscountAmount: 0m,
            TotalAmount: 75.00m,
            Currency: "USD",
            TxnDate: DateTimeOffset.UtcNow,
            Lines: []);

        var message = OutboxMessage.Create(
            OutboxMessageType.QuickBooksInvoiceSync,
            "Order",
            orderId.ToString(),
            orderId.ToString(),
            JsonSerializer.Serialize(invoiceRequest));

        message.ClaimForProcessing();

        // Act
        await handler.HandleAsync(message, CancellationToken.None);

        // Assert: Existing mapping remains unchanged, preserving idempotency
        var map = await repo.GetByLocalEntityAsync(orderId, QuickBooksSyncEntityType.Invoice);
        Assert.NotNull(map);
        Assert.Equal("QB-TXN-EXISTING", map.QuickBooksTxnId);
        Assert.Equal(QuickBooksSyncStatus.Synchronized, map.Status);
    }

    [Fact]
    public async Task InvoiceSyncHandler_ServiceUnavailable_RecordsFailure_AndThrowsForOutboxRetry()
    {
        // Arrange
        var qbGateway = new DefaultQuickBooksGateway();
        qbGateway.SetSimulatedOutage(true);

        var cbRegistry = new CircuitBreakerRegistry();
        var repo = new FakeQuickBooksSyncMapRepository();
        var handler = new QuickBooksInvoiceSyncHandler(
            qbGateway,
            cbRegistry,
            NullLogger<QuickBooksInvoiceSyncHandler>.Instance,
            repo);

        var orderId = Guid.NewGuid();
        var invoiceRequest = new QuickBooksInvoiceRequest(
            OrderId: orderId,
            OrderNumber: "ORD-FAIL-1",
            CustomerName: "Crash Test",
            SubTotal: 50.00m,
            TaxAmount: 0m,
            DiscountAmount: 0m,
            TotalAmount: 50.00m,
            Currency: "USD",
            TxnDate: DateTimeOffset.UtcNow,
            Lines: []);

        var message = OutboxMessage.Create(
            OutboxMessageType.QuickBooksInvoiceSync,
            "Order",
            orderId.ToString(),
            orderId.ToString(),
            JsonSerializer.Serialize(invoiceRequest));

        message.ClaimForProcessing();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(message, CancellationToken.None));
        Assert.Contains("503", ex.Message);

        var map = await repo.GetByLocalEntityAsync(orderId, QuickBooksSyncEntityType.Invoice);
        Assert.NotNull(map);
        Assert.Equal(QuickBooksSyncStatus.Failed, map.Status);
        Assert.Contains("503", map.LastError);
        Assert.Equal(1, map.RetryCount);
    }

    [Fact]
    public async Task InvoiceSyncHandler_LegacySyncPayload_DeserializesAndSynchronizes()
    {
        // Arrange
        var qbGateway = new DefaultQuickBooksGateway();
        var cbRegistry = new CircuitBreakerRegistry();
        var repo = new FakeQuickBooksSyncMapRepository();
        var handler = new QuickBooksInvoiceSyncHandler(
            qbGateway,
            cbRegistry,
            NullLogger<QuickBooksInvoiceSyncHandler>.Instance,
            repo);

        var orderId = Guid.NewGuid();
        var legacyPayload = new QuickBooksSyncPayload(
            OrderId: orderId,
            OrderNumber: "LEGACY-101",
            TotalAmount: 120.50m,
            PaymentMethod: "Cash",
            CustomerName: "Legacy Customer",
            CompletedAtUtc: DateTimeOffset.UtcNow);

        var message = OutboxMessage.Create(
            OutboxMessageType.QuickBooksInvoiceSync,
            "Order",
            orderId.ToString(),
            orderId.ToString(),
            JsonSerializer.Serialize(legacyPayload));

        message.ClaimForProcessing();

        // Act
        await handler.HandleAsync(message, CancellationToken.None);

        // Assert
        var map = await repo.GetByLocalEntityAsync(orderId, QuickBooksSyncEntityType.Invoice);
        Assert.NotNull(map);
        Assert.Equal(QuickBooksSyncStatus.Synchronized, map.Status);
        Assert.Equal(120.50m, map.Amount);
    }

    [Fact]
    public async Task PaymentSyncHandler_SuccessfulSync_LinksToInvoiceTxnId_CreatesSynchronizedMap()
    {
        // Arrange
        var qbGateway = new DefaultQuickBooksGateway();
        var cbRegistry = new CircuitBreakerRegistry();
        var repo = new FakeQuickBooksSyncMapRepository();

        var orderId = Guid.NewGuid();
        var invoiceMap = QuickBooksSyncMap.Create(
            localEntityId: orderId,
            entityType: QuickBooksSyncEntityType.Invoice,
            amount: 80.00m,
            currency: "USD");
        invoiceMap.MarkSynchronized("QB-TXN-INV-888", "INV-888");
        await repo.AddAsync(invoiceMap);

        var handler = new QuickBooksPaymentSyncHandler(
            qbGateway,
            cbRegistry,
            NullLogger<QuickBooksPaymentSyncHandler>.Instance,
            repo);

        var paymentId = Guid.NewGuid();
        var paymentRequest = new QuickBooksPaymentRequest(
            PaymentId: paymentId,
            OrderId: orderId,
            OrderNumber: "ORD-888",
            PaymentMethod: "CreditCard",
            Amount: 80.00m,
            PaymentReference: "REF-CARD-123",
            PaymentDate: DateTimeOffset.UtcNow,
            QuickBooksInvoiceTxnId: null); // omitted to test automatic link lookup

        var message = OutboxMessage.Create(
            OutboxMessageType.QuickBooksPaymentSync,
            "Payment",
            paymentId.ToString(),
            paymentId.ToString(),
            JsonSerializer.Serialize(paymentRequest));

        message.ClaimForProcessing();

        // Act
        await handler.HandleAsync(message, CancellationToken.None);

        // Assert
        var paymentSyncMap = await repo.GetByLocalEntityAsync(paymentId, QuickBooksSyncEntityType.Payment);
        Assert.NotNull(paymentSyncMap);
        Assert.Equal(QuickBooksSyncStatus.Synchronized, paymentSyncMap.Status);
        Assert.NotNull(paymentSyncMap.QuickBooksTxnId);
        Assert.StartsWith("QB-TXN-PAY-", paymentSyncMap.QuickBooksTxnId);
    }

    [Fact]
    public async Task PaymentSyncHandler_AlreadySynchronized_SkipsDuplicatePost()
    {
        // Arrange
        var qbGateway = new DefaultQuickBooksGateway();
        var cbRegistry = new CircuitBreakerRegistry();
        var repo = new FakeQuickBooksSyncMapRepository();

        var paymentId = Guid.NewGuid();
        var existingMap = QuickBooksSyncMap.Create(
            localEntityId: paymentId,
            entityType: QuickBooksSyncEntityType.Payment,
            amount: 40.00m,
            currency: "USD");
        existingMap.MarkSynchronized("QB-TXN-PAY-EXISTING", "PAY-999");
        await repo.AddAsync(existingMap);

        var handler = new QuickBooksPaymentSyncHandler(
            qbGateway,
            cbRegistry,
            NullLogger<QuickBooksPaymentSyncHandler>.Instance,
            repo);

        var paymentRequest = new QuickBooksPaymentRequest(
            PaymentId: paymentId,
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-999",
            PaymentMethod: "Cash",
            Amount: 40.00m,
            PaymentReference: null,
            PaymentDate: DateTimeOffset.UtcNow);

        var message = OutboxMessage.Create(
            OutboxMessageType.QuickBooksPaymentSync,
            "Payment",
            paymentId.ToString(),
            paymentId.ToString(),
            JsonSerializer.Serialize(paymentRequest));

        message.ClaimForProcessing();

        // Act
        await handler.HandleAsync(message, CancellationToken.None);

        // Assert
        var paymentSyncMap = await repo.GetByLocalEntityAsync(paymentId, QuickBooksSyncEntityType.Payment);
        Assert.NotNull(paymentSyncMap);
        Assert.Equal("QB-TXN-PAY-EXISTING", paymentSyncMap.QuickBooksTxnId);
    }

    [Fact]
    public async Task ShiftSyncHandler_SuccessfulSync_CreatesSynchronizedMap()
    {
        // Arrange
        var qbGateway = new DefaultQuickBooksGateway();
        var cbRegistry = new CircuitBreakerRegistry();
        var repo = new FakeQuickBooksSyncMapRepository();
        var handler = new QuickBooksShiftSyncHandler(
            qbGateway,
            cbRegistry,
            NullLogger<QuickBooksShiftSyncHandler>.Instance,
            repo);

        var shiftId = Guid.NewGuid();
        var shiftRequest = new QuickBooksShiftSummaryRequest(
            ShiftId: shiftId,
            ShiftNumber: 5,
            TerminalId: Guid.NewGuid(),
            CashierName: "Alice",
            TotalSales: 1500.00m,
            CashTendered: 1200.00m,
            CashVariance: 0.00m,
            OrderCount: 42,
            ClosedAtUtc: DateTimeOffset.UtcNow);

        var message = OutboxMessage.Create(
            OutboxMessageType.QuickBooksShiftSync,
            "Shift",
            shiftId.ToString(),
            shiftId.ToString(),
            JsonSerializer.Serialize(shiftRequest));

        message.ClaimForProcessing();

        // Act
        await handler.HandleAsync(message, CancellationToken.None);

        // Assert
        var shiftMap = await repo.GetByLocalEntityAsync(shiftId, QuickBooksSyncEntityType.ShiftSummary);
        Assert.NotNull(shiftMap);
        Assert.Equal(QuickBooksSyncStatus.Synchronized, shiftMap.Status);
        Assert.NotNull(shiftMap.QuickBooksTxnId);
        Assert.StartsWith("QB-TXN-GENJNL-", shiftMap.QuickBooksTxnId);
        Assert.Equal(1500.00m, shiftMap.Amount);
    }

    [Fact]
    public async Task ShiftSyncHandler_Outage_MarksFailed_ThrowsException()
    {
        // Arrange
        var qbGateway = new DefaultQuickBooksGateway();
        qbGateway.SetSimulatedOutage(true);

        var cbRegistry = new CircuitBreakerRegistry();
        var repo = new FakeQuickBooksSyncMapRepository();
        var handler = new QuickBooksShiftSyncHandler(
            qbGateway,
            cbRegistry,
            NullLogger<QuickBooksShiftSyncHandler>.Instance,
            repo);

        var shiftId = Guid.NewGuid();
        var shiftRequest = new QuickBooksShiftSummaryRequest(
            ShiftId: shiftId,
            ShiftNumber: 6,
            TerminalId: Guid.NewGuid(),
            CashierName: "Bob",
            TotalSales: 800.00m,
            CashTendered: 800.00m,
            CashVariance: 0.00m,
            OrderCount: 15,
            ClosedAtUtc: DateTimeOffset.UtcNow);

        var message = OutboxMessage.Create(
            OutboxMessageType.QuickBooksShiftSync,
            "Shift",
            shiftId.ToString(),
            shiftId.ToString(),
            JsonSerializer.Serialize(shiftRequest));

        message.ClaimForProcessing();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(message, CancellationToken.None));

        var shiftMap = await repo.GetByLocalEntityAsync(shiftId, QuickBooksSyncEntityType.ShiftSummary);
        Assert.NotNull(shiftMap);
        Assert.Equal(QuickBooksSyncStatus.Failed, shiftMap.Status);
        Assert.Equal(1, shiftMap.RetryCount);
    }
}
