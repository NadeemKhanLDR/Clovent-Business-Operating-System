using Clovent.Restaurant.Application.Continuity;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Continuity;

public sealed class EmergencyJournalReplayerTests
{
    private readonly FakeContinuityJournalStore _journalStore = new();
    private readonly FakeOrderRepository _orderRepo = new();
    private readonly FakeOrderLineRepository _lineRepo = new();
    private readonly FakePaymentRepository _paymentRepo = new();
    private readonly FakePaymentMethodRepository _methodRepo = new();
    private readonly FakeOutboxRepository _outboxRepo = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private EmergencyJournalReplayer CreateReplayer()
    {
        return new EmergencyJournalReplayer(
            _journalStore,
            _orderRepo,
            _lineRepo,
            _paymentRepo,
            _methodRepo,
            _unitOfWork,
            _outboxRepo);
    }

    private async Task SeedCashPaymentMethodAsync()
    {
        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        await _methodRepo.AddAsync(cashMethod);
    }

    [Fact]
    public async Task ReplayPendingAsync_WhenEmpty_ReturnsZeroCounts()
    {
        var replayer = CreateReplayer();
        var result = await replayer.ReplayPendingAsync();

        Assert.Equal(0, result.TotalProcessed);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.DuplicateIgnoredCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task ReplayPendingAsync_ValidTransaction_ReplaysToDatabaseAndEnqueuesOutbox()
    {
        await SeedCashPaymentMethodAsync();

        var txId = Guid.NewGuid();
        var seq = 1L;
        var now = DateTimeOffset.UtcNow;
        var terminalId = "POS-01";
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var cashierName = "Cashier Bob";
        var grandTotal = 250m;
        var paymentType = "Cash";

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);
        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, EmergencyTransaction.GenesisHash, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

        var snapshot = new EmergencyOrderSnapshot(
            "TakeAway", null, null,
            [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-BURGER", "Beef Burger", 1, 250m, 250m, null)],
            250m, 0m, 0m, 0m, grandTotal, "Customer note", null);

        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = EmergencyTransaction.GenesisHash,
            TimestampUtc = now,
            TerminalId = terminalId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            CashierId = Guid.NewGuid(),
            CashierName = cashierName,
            OrderSnapshot = snapshot,
            PaymentType = paymentType,
            AmountTendered = 300m,
            ChangeGiven = 50m,
            Checksum = checksum,
            HmacSignature = hmac,
            ReconciliationStatus = ReconciliationStatus.PendingReplay
        };
        await _journalStore.AppendAsync(tx);

        var replayer = CreateReplayer();
        var result = await replayer.ReplayPendingAsync();

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);

        // Verify order created
        var orders = await _orderRepo.GetAllAsync();
        Assert.Single(orders);
        var order = orders.First();
        Assert.NotNull(order.ReceiptSnapshotJson);

        // Verify payment created with emergency idempotency key
        var payments = await _paymentRepo.GetByOrderIdAsync(order.Id);
        Assert.Single(payments);
        var payment = payments.First();
        Assert.Equal($"emergency:{txId}", payment.IdempotencyKey);
        Assert.Equal(250m, payment.Amount);

        // Verify outbox messages enqueued
        Assert.True(_outboxRepo.AllMessages.Count >= 4);
        Assert.Contains(_outboxRepo.AllMessages, m => m.MessageType == OutboxMessageType.InventoryPosting);
        Assert.Contains(_outboxRepo.AllMessages, m => m.MessageType == OutboxMessageType.QuickBooksSync);

        // Verify UnitOfWork saved
        Assert.True(_unitOfWork.SaveCount >= 1);

        // Verify journal updated to Replayed
        var updatedJournal = await _journalStore.GetAllAsync();
        var updatedTx = updatedJournal.First(t => t.TransactionId == txId);
        Assert.Equal(ReconciliationStatus.Replayed, updatedTx.ReconciliationStatus);
        Assert.NotNull(updatedTx.ReconciledAtUtc);
    }

    [Fact]
    public async Task ReplayPendingAsync_DuplicateTransaction_MarksDuplicateIgnored()
    {
        await SeedCashPaymentMethodAsync();

        var txId = Guid.NewGuid();
        var seq = 2L;
        var now = DateTimeOffset.UtcNow;
        var terminalId = "POS-01";
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var cashierName = "Cashier Bob";
        var grandTotal = 100m;
        var paymentType = "Cash";

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);
        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, EmergencyTransaction.GenesisHash, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

        var snapshot = new EmergencyOrderSnapshot(
            "TakeAway", null, null,
            [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-TEA", "Chai", 1, 100m, 100m, null)],
            100m, 0m, 0m, 0m, grandTotal, null, null);

        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = EmergencyTransaction.GenesisHash,
            TimestampUtc = now,
            TerminalId = terminalId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            CashierId = Guid.NewGuid(),
            CashierName = cashierName,
            OrderSnapshot = snapshot,
            PaymentType = paymentType,
            AmountTendered = 100m,
            ChangeGiven = 0m,
            Checksum = checksum,
            HmacSignature = hmac,
            ReconciliationStatus = ReconciliationStatus.PendingReplay
        };
        await _journalStore.AppendAsync(tx);

        // Simulate pre-existing payment with this emergency idempotency key
        var cashMethod = (await _methodRepo.GetAllAsync()).First();
        var existingPayment = Payment.Create(
            new Clovent.Restaurant.Orders.OrderId(Guid.NewGuid()),
            cashMethod.Id,
            100m,
            null,
            $"emergency:{txId}");
        await _paymentRepo.AddAsync(existingPayment);

        var replayer = CreateReplayer();
        var result = await replayer.ReplayPendingAsync();

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.DuplicateIgnoredCount);
        Assert.Equal(0, result.FailedCount);

        var updatedTx = (await _journalStore.GetAllAsync()).First();
        Assert.Equal(ReconciliationStatus.DuplicateIgnored, updatedTx.ReconciliationStatus);
    }

    [Fact]
    public async Task ReplayPendingAsync_TamperedChecksum_MarksConflictAndFails()
    {
        await SeedCashPaymentMethodAsync();

        var txId = Guid.NewGuid();
        var seq = 3L;
        var now = DateTimeOffset.UtcNow;
        var terminalId = "POS-01";
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var cashierName = "Cashier Bob";
        var grandTotal = 100m;
        var paymentType = "Cash";

        var validChecksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);
        var validHmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, EmergencyTransaction.GenesisHash, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

        // Tamper grand total so checksum will not verify
        var tamperedSnapshot = new EmergencyOrderSnapshot(
            "TakeAway", null, null,
            [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-TEA", "Chai", 1, 999m, 999m, null)],
            999m, 0m, 0m, 0m, 999m, null, null);

        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = EmergencyTransaction.GenesisHash,
            TimestampUtc = now,
            TerminalId = terminalId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            CashierId = Guid.NewGuid(),
            CashierName = cashierName,
            OrderSnapshot = tamperedSnapshot,
            PaymentType = paymentType,
            AmountTendered = 100m,
            ChangeGiven = 0m,
            Checksum = validChecksum, // Invalid for 999m
            HmacSignature = validHmac, // Also invalid for 999m
            ReconciliationStatus = ReconciliationStatus.PendingReplay
        };
        await _journalStore.AppendAsync(tx);

        var replayer = CreateReplayer();
        var result = await replayer.ReplayPendingAsync();

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);

        var updatedTx = (await _journalStore.GetAllAsync()).First();
        Assert.Equal(ReconciliationStatus.Conflict, updatedTx.ReconciliationStatus);
        Assert.Contains("Tamper check failed", updatedTx.ReconciliationDetails);
    }

    [Fact]
    public async Task ReplayPendingAsync_WithLocalReceiptNumber_PreservesIdentifierAndDetails()
    {
        await SeedCashPaymentMethodAsync();

        var txId = Guid.NewGuid();
        var seq = 5L;
        var now = DateTimeOffset.UtcNow;
        var terminalId = "POS-01";
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var cashierName = "Cashier Bob";
        var grandTotal = 300m;
        var paymentType = "Cash";

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);
        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, EmergencyTransaction.GenesisHash, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

        var snapshot = new EmergencyOrderSnapshot(
            "TakeAway", null, null,
            [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-BURGER", "Beef Burger", 1, 300m, 300m, null, 16.0m, 48m, false, 0m, "Prepared")],
            300m, 48m, 0m, 0m, grandTotal, "Fast service", null);

        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = EmergencyTransaction.GenesisHash,
            TimestampUtc = now,
            TerminalId = terminalId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            CashierId = Guid.NewGuid(),
            CashierName = cashierName,
            OrderSnapshot = snapshot,
            PaymentType = paymentType,
            AmountTendered = 500m,
            ChangeGiven = 200m,
            Checksum = checksum,
            HmacSignature = hmac,
            LocalReceiptNumber = "CONT-POS01-00005",
            CacheVersion = "1.2.0",
            ReconciliationStatus = ReconciliationStatus.PendingReplay
        };
        await _journalStore.AppendAsync(tx);

        var replayer = CreateReplayer();
        var result = await replayer.ReplayPendingAsync();

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(1, result.SuccessCount);

        var updatedTx = (await _journalStore.GetAllAsync()).First();
        Assert.Equal(ReconciliationStatus.Replayed, updatedTx.ReconciliationStatus);
        Assert.Contains("CONT-POS01-00005", updatedTx.ReconciliationDetails);

        var orders = await _orderRepo.GetAllAsync();
        var order = orders.First();
        Assert.Contains("CONT-POS01-00005", order.Notes);
    }

    [Fact]
    public async Task ReplayPendingAsync_MissingHmacSignature_MarksConflictAndFails()
    {
        await SeedCashPaymentMethodAsync();

        var txId = Guid.NewGuid();
        var seq = 6L;
        var now = DateTimeOffset.UtcNow;
        var grandTotal = 150m;

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, "POS-01", Guid.NewGuid(), Guid.NewGuid(), "Cashier", grandTotal, "Cash");

        var snapshot = new EmergencyOrderSnapshot(
            "TakeAway", null, null,
            [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-PIZZA", "Pizza", 1, grandTotal, grandTotal, null)],
            grandTotal, 0, 0, 0, grandTotal, null, null);

        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = EmergencyTransaction.GenesisHash,
            TimestampUtc = now,
            TerminalId = "POS-01",
            BranchId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            CashierName = "Cashier",
            OrderSnapshot = snapshot,
            PaymentType = "Cash",
            AmountTendered = grandTotal,
            Checksum = checksum,
            HmacSignature = string.Empty, // Missing HMAC
            ReconciliationStatus = ReconciliationStatus.PendingReplay
        };
        await _journalStore.AppendAsync(tx);

        var replayer = CreateReplayer();
        var result = await replayer.ReplayPendingAsync();

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);

        var updatedTx = (await _journalStore.GetAllAsync()).First();
        Assert.Equal(ReconciliationStatus.Conflict, updatedTx.ReconciliationStatus);
        Assert.Contains("Tamper check failed", updatedTx.ReconciliationDetails);
    }

    [Fact]
    public async Task ReplayPendingAsync_TamperedHmacSignature_MarksConflictAndFails()
    {
        await SeedCashPaymentMethodAsync();

        var txId = Guid.NewGuid();
        var seq = 7L;
        var now = DateTimeOffset.UtcNow;
        var grandTotal = 200m;

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, "POS-01", Guid.NewGuid(), Guid.NewGuid(), "Cashier", grandTotal, "Cash");

        var snapshot = new EmergencyOrderSnapshot(
            "TakeAway", null, null,
            [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-BURGER", "Burger", 1, grandTotal, grandTotal, null)],
            grandTotal, 0, 0, 0, grandTotal, null, null);

        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = EmergencyTransaction.GenesisHash,
            TimestampUtc = now,
            TerminalId = "POS-01",
            BranchId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            CashierName = "Cashier",
            OrderSnapshot = snapshot,
            PaymentType = "Cash",
            AmountTendered = grandTotal,
            Checksum = checksum,
            HmacSignature = "BAD_HMAC_SIGNATURE_TAMPERED",
            ReconciliationStatus = ReconciliationStatus.PendingReplay
        };
        await _journalStore.AppendAsync(tx);

        var replayer = CreateReplayer();
        var result = await replayer.ReplayPendingAsync();

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);

        var updatedTx = (await _journalStore.GetAllAsync()).First();
        Assert.Equal(ReconciliationStatus.Conflict, updatedTx.ReconciliationStatus);
        Assert.Contains("Tamper check failed", updatedTx.ReconciliationDetails);
    }
}
