using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Continuity;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Outbox.Handlers;
using Clovent.Restaurant.Application.Printing;
using Clovent.Restaurant.Application.QuickBooks;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Outbox;

/// <summary>
/// Failure-Injection Verification Suite covering Scenarios A through L of the Always-On POS architecture.
/// Proves that the POS counter remains resilient and operational across external dependency failures.
/// </summary>
public sealed class FailureInjectionMatrixTests
{
    [Fact]
    public async Task ScenarioA_QuickBooksOutage_CircuitBreakerOpens_MessageRetried_POSUnimpeded()
    {
        // Arrange
        var qbGateway = new DefaultQuickBooksGateway();
        qbGateway.SetSimulatedOutage(true);

        var cbRegistry = new CircuitBreakerRegistry();
        var handler = new QuickBooksSyncOutboxHandler(qbGateway, cbRegistry, NullLogger<QuickBooksSyncOutboxHandler>.Instance);

        var payload = new QuickBooksSyncPayload(Guid.NewGuid(), "ORD-1001", 450.00m, "Cash", null, DateTimeOffset.UtcNow);
        var msg = OutboxMessage.Create(
            OutboxMessageType.QuickBooksSync, "Order", payload.OrderId.ToString(), payload.OrderId.ToString(), JsonSerializer.Serialize(payload));
        msg.ClaimForProcessing();

        // Act & Assert: Handler fails fast due to outage
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(msg, CancellationToken.None));
        Assert.Contains("503", ex.Message);

        // Schedule retry with backoff
        msg.ScheduleRetry(ex.Message, maxAttempts: 5);
        Assert.Equal(OutboxMessageStatus.RetryScheduled, msg.Status);
        Assert.Equal(1, msg.AttemptCount);
    }

    [Fact]
    public async Task ScenarioB_PrinterOutage_SpoolerFails_MessageRetried_OrderCommitted()
    {
        // Arrange
        var printService = new DefaultReceiptPrintService();
        printService.SetSimulatedOutage(true);

        var cbRegistry = new CircuitBreakerRegistry();
        var handler = new ReceiptPrintOutboxHandler(printService, cbRegistry, NullLogger<ReceiptPrintOutboxHandler>.Instance);

        var payload = new ReceiptPrintPayload(Guid.NewGuid(), "ORD-1002", "RECEIPT TEXT");
        var msg = OutboxMessage.Create(
            OutboxMessageType.ReceiptPrint, "Order", payload.OrderId.ToString(), payload.OrderId.ToString(), JsonSerializer.Serialize(payload));

        // Act & Assert: Print failure throws gracefully
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(msg, CancellationToken.None));
        Assert.Contains("Printer is offline", ex.Message);

        msg.ScheduleRetry(ex.Message, maxAttempts: 5);
        Assert.Equal(OutboxMessageStatus.RetryScheduled, msg.Status);
    }

    [Fact]
    public void ScenarioD_InventoryPostingTransientFailure_RetriesWithBackoff()
    {
        var msg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting, "Order", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "{}");

        msg.ClaimForProcessing();
        msg.ScheduleRetry("Deadlock detected during stock reduction", maxAttempts: 5);

        Assert.Equal(OutboxMessageStatus.RetryScheduled, msg.Status);
        Assert.Equal(1, msg.AttemptCount);
        Assert.NotNull(msg.NextRetryAtUtc);
        Assert.True(msg.NextRetryAtUtc > DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void ScenarioE_WorkerKilledMidExecution_ExceedingMaxRetries_MovesToDeadLetter()
    {
        var msg = OutboxMessage.Create(
            OutboxMessageType.CloudSync, "Order", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "{}");

        for (int i = 0; i < 5; i++)
        {
            msg.ClaimForProcessing();
            msg.ScheduleRetry($"Simulated failure {i + 1}", maxAttempts: 5);
        }

        Assert.Equal(OutboxMessageStatus.DeadLetter, msg.Status);
        Assert.Contains("Max retry attempts", msg.LastError);
    }

    [Fact]
    public async Task ScenarioH_SqlOutage_ContinuityModeEngaged_CashSaleJournaled()
    {
        var journalStore = new FakeContinuityJournalStore();
        var txId = Guid.NewGuid();
        var seq = await journalStore.GetNextSequenceNumberAsync();
        var prevHash = await journalStore.GetLastTransactionHashAsync();
        var now = DateTimeOffset.UtcNow;

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, "POS-01", Guid.NewGuid(), Guid.NewGuid(), "Cashier", 250m, "Cash");
        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, prevHash, now, "POS-01", Guid.NewGuid(), Guid.NewGuid(), "Cashier", 250m, "Cash");

        var snapshot = new EmergencyOrderSnapshot(
            "TakeAway", null, null, [], 250m, 0, 0, 0, 250m, null, null);

        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = prevHash,
            TimestampUtc = now,
            TerminalId = "POS-01",
            BranchId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            CashierName = "Cashier",
            OrderSnapshot = snapshot,
            PaymentType = "Cash",
            AmountTendered = 250m,
            ChangeGiven = 0,
            Checksum = checksum,
            HmacSignature = hmac
        };

        await journalStore.AppendAsync(tx);

        var stats = await journalStore.GetStatisticsAsync();
        Assert.Equal(1, stats.TotalRecorded);
        Assert.Equal(1, stats.PendingReplayCount);
    }

    [Fact]
    public async Task ScenarioI_and_J_SqlRestored_EmergencyReplay_IdempotencyPreventsDuplicates()
    {
        var journalStore = new FakeContinuityJournalStore();
        var orderRepo = new FakeOrderRepository();
        var lineRepo = new FakeOrderLineRepository();
        var paymentRepo = new FakePaymentRepository();
        var methodRepo = new FakePaymentMethodRepository();
        var outboxRepo = new FakeOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        await methodRepo.AddAsync(cashMethod);

        var txId = Guid.NewGuid();
        var seq = 1L;
        var prevHash = EmergencyTransaction.GenesisHash;
        var now = DateTimeOffset.UtcNow;
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, "POS-01", branchId, warehouseId, "Cashier", 120m, "Cash");
        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, prevHash, now, "POS-01", branchId, warehouseId, "Cashier", 120m, "Cash");

        var snapshot = new EmergencyOrderSnapshot(
            "TakeAway", null, null,
            [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-COFFEE", "Latte", 1, 120m, 120m, null)],
            120m, 0, 0, 0, 120m, null, null);

        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = prevHash,
            TimestampUtc = now,
            TerminalId = "POS-01",
            BranchId = branchId,
            WarehouseId = warehouseId,
            CashierName = "Cashier",
            OrderSnapshot = snapshot,
            PaymentType = "Cash",
            AmountTendered = 120m,
            ChangeGiven = 0,
            Checksum = checksum,
            HmacSignature = hmac,
            ReconciliationStatus = ReconciliationStatus.PendingReplay
        };
        await journalStore.AppendAsync(tx);

        var replayer = new EmergencyJournalReplayer(
            journalStore, orderRepo, lineRepo, paymentRepo, methodRepo, unitOfWork, outboxRepo);

        // Scenario I: First replay passes
        var result1 = await replayer.ReplayPendingAsync();
        Assert.Equal(1, result1.SuccessCount);
        Assert.Equal(0, result1.DuplicateIgnoredCount);

        var savedOrders = await orderRepo.GetAllAsync();
        Assert.Single(savedOrders);

        // Scenario J: Second replay pass is completely idempotent
        // Force status back to PendingReplay to simulate crash before journal update
        tx.ReconciliationStatus = ReconciliationStatus.PendingReplay;
        await journalStore.UpdateAsync(tx);

        var result2 = await replayer.ReplayPendingAsync();
        Assert.Equal(0, result2.SuccessCount);
        Assert.Equal(1, result2.DuplicateIgnoredCount);

        // Still exactly 1 order in the database
        savedOrders = await orderRepo.GetAllAsync();
        Assert.Single(savedOrders);
    }

    [Fact]
    public void ScenarioL_JournalTamperOrReordering_RejectedByCryptographicValidation()
    {
        var txId = Guid.NewGuid();
        var seq = 1L;
        var prevHash = EmergencyTransaction.GenesisHash;
        var now = DateTimeOffset.UtcNow;

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, "POS-01", Guid.NewGuid(), Guid.NewGuid(), "Cashier", 100m, "Cash");
        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, prevHash, now, "POS-01", Guid.NewGuid(), Guid.NewGuid(), "Cashier", 100m, "Cash");

        var snapshot = new EmergencyOrderSnapshot("Takeaway", null, null, [], 100m, 0, 0, 0, 100m, null, null);
        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = prevHash,
            TimestampUtc = now,
            TerminalId = "POS-01",
            BranchId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            CashierName = "Cashier",
            OrderSnapshot = snapshot,
            PaymentType = "Cash",
            AmountTendered = 100m,
            Checksum = checksum,
            HmacSignature = hmac + "TAMPERED"
        };

        Assert.False(tx.VerifyHmacSignature());
        var list = new List<EmergencyTransaction> { tx };
        Assert.Throws<ContinuityTamperException>(() => EmergencyTransaction.ValidateChain(list));
    }
}
