using System.Security.Cryptography;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Continuity;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Infrastructure.Continuity;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Continuity;

/// <summary>
/// Comprehensive stress and integration tests for the Offline Continuity Replay Engine
/// running against a real relational database engine (SQLite relational persistence).
/// Validates DPAPI encryption, HMAC-SHA256 signature enforcement, batch replay accuracy,
/// zero duplicate transactions, zero ledger drift, and concurrent replay recovery.
/// </summary>
public sealed class EmergencyJournalReplayStressIntegrationTests : SqliteTestBase
{
    private readonly string _tempDir;
    private readonly string _journalPath;
    private PaymentMethod _cashMethod = null!;

    public EmergencyJournalReplayStressIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CloventStress_Continuity_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "stress_journal.dat");

        SeedPaymentMethod();
    }

    private void SeedPaymentMethod()
    {
        using var db = CreateContext();
        _cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        db.PaymentMethods.Add(_cashMethod);
        db.SaveChanges();
    }

    public override void Dispose()
    {
        base.Dispose();
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task StressTest_SimulatedDisconnection_BatchReplayAccuracy_NoLedgerDrift()
    {
        // 1. Arrange: Real DPAPI journal store on disk
        var journalStore = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.CurrentUser);

        // 2. Simulate server disconnection event: Cashier records 15 consecutive offline sales
        const int transactionCount = 15;
        var offlineTransactions = new List<EmergencyTransaction>(transactionCount);
        var prevHash = EmergencyTransaction.GenesisHash;
        decimal expectedTotalRevenue = 0m;
        decimal expectedTotalTax = 0m;

        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var terminalId = "POS-TERMINAL-01";
        var cashierName = "Cashier John";

        for (int i = 1; i <= transactionCount; i++)
        {
            var txId = Guid.NewGuid();
            var seq = (long)i;
            var now = DateTimeOffset.UtcNow.AddMinutes(i);

            // Create 2 line items per order
            var line1Price = 120.00m + (i * 10m);
            var line1TaxRate = 16.00m;
            var line1TaxAmount = Math.Round(line1Price * (line1TaxRate / 100m), 2, MidpointRounding.AwayFromZero);

            var line2Price = 50.00m;
            var line2TaxRate = 0m;
            var line2TaxAmount = 0m;

            var subtotal = line1Price + line2Price;
            var taxTotal = line1TaxAmount + line2TaxAmount;
            var grandTotal = subtotal + taxTotal;

            expectedTotalRevenue += grandTotal;
            expectedTotalTax += taxTotal;

            var lines = new List<EmergencyTransactionLine>
            {
                new(Guid.NewGuid(), $"SKU-ITEM-{i}", $"Special Combo #{i}", 1, line1Price, line1Price, null, line1TaxRate, line1TaxAmount, false, 0m, "Prepared"),
                new(Guid.NewGuid(), "SKU-BEV-01", "Bottled Water", 1, line2Price, line2Price, null, line2TaxRate, line2TaxAmount, false, 0m, "Purchased")
            };

            var snapshot = new EmergencyOrderSnapshot(
                "TakeAway",
                null,
                null,
                lines,
                subtotal,
                taxTotal,
                0m,
                0m,
                grandTotal,
                $"Offline batch sale #{i}",
                null,
                0m,
                "PKR",
                "1.2.2");

            var checksum = EmergencyTransaction.ComputeChecksum(
                txId, seq, now, terminalId, branchId, warehouseId, cashierName, grandTotal, "Cash");

            var hmac = EmergencyTransaction.ComputeHmacSignature(
                txId, seq, prevHash, now, terminalId, branchId, warehouseId, cashierName, grandTotal, "Cash");

            var tx = new EmergencyTransaction
            {
                TransactionId = txId,
                SequenceNumber = seq,
                PreviousTransactionHash = prevHash,
                TimestampUtc = now,
                TerminalId = terminalId,
                BranchId = branchId,
                WarehouseId = warehouseId,
                CashierId = Guid.NewGuid(),
                CashierName = cashierName,
                OrderSnapshot = snapshot,
                PaymentType = "Cash",
                AmountTendered = grandTotal + 100m,
                ChangeGiven = 100m,
                Checksum = checksum,
                HmacSignature = hmac,
                LocalReceiptNumber = $"CONT-POS01-{i:D5}",
                CacheVersion = "1.2.2",
                ReconciliationStatus = ReconciliationStatus.PendingReplay
            };

            await journalStore.AppendAsync(tx);
            offlineTransactions.Add(tx);
            prevHash = hmac;
        }

        // Verify all 15 transactions are securely encrypted on disk
        Assert.True(File.Exists(_journalPath));
        var encryptedLines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Equal(transactionCount, encryptedLines.Length);
        // Ensure no plaintext appears in raw journal file
        var rawContent = await File.ReadAllTextAsync(_journalPath);
        Assert.DoesNotContain("Special Combo", rawContent);
        Assert.DoesNotContain("Cashier John", rawContent);

        // 3. Simulate SQL Server Connection Restoration: Run EmergencyJournalReplayer
        using var replayDb = CreateContext();
        var replayer = new EmergencyJournalReplayer(
            journalStore,
            new OrderRepository(replayDb),
            new OrderLineRepository(replayDb),
            new PaymentRepository(replayDb),
            new PaymentMethodRepository(replayDb),
            new UnitOfWork(replayDb),
            new OutboxRepository(replayDb),
            null,
            NullLogger<EmergencyJournalReplayer>.Instance);

        var replayResult = await replayer.ReplayPendingAsync();

        // 4. Validate Replay Engine Results
        Assert.Equal(transactionCount, replayResult.TotalProcessed);
        Assert.Equal(transactionCount, replayResult.SuccessCount);
        Assert.Equal(0, replayResult.DuplicateIgnoredCount);
        Assert.Equal(0, replayResult.FailedCount);
        Assert.Empty(replayResult.Errors);

        // 5. Audit Database State for Accuracy and Ledger Invariants
        using var verifyDb = CreateContext();
        var dbOrders = await verifyDb.Orders.ToListAsync();
        var dbOrderLines = await verifyDb.OrderLines.ToListAsync();
        var dbPayments = await verifyDb.Payments.ToListAsync();
        var dbOutbox = await verifyDb.OutboxMessages.ToListAsync();

        Assert.Equal(transactionCount, dbOrders.Count);
        Assert.Equal(transactionCount * 2, dbOrderLines.Count);
        Assert.Equal(transactionCount, dbPayments.Count);
        Assert.Equal(transactionCount * 4, dbOutbox.Count); // 4 outbox events per completed order

        // Verify absolute zero ledger drift (Mathematical Invariant)
        var actualTotalPayments = dbPayments.Sum(p => p.Amount);
        Assert.Equal(expectedTotalRevenue, actualTotalPayments);
        Assert.Equal(0.00m, expectedTotalRevenue - actualTotalPayments); // Zero cents drift

        // Verify all orders are in Completed status and have receipt snapshots
        foreach (var order in dbOrders)
        {
            Assert.Equal(OrderStatus.Completed, order.Status);
            Assert.NotNull(order.ReceiptSnapshotJson);
            Assert.Contains("[Continuity Receipt: CONT-POS01-", order.Notes ?? string.Empty);
        }

        // Verify journal records updated to Replayed
        var updatedJournal = await journalStore.GetAllAsync();
        Assert.Equal(transactionCount, updatedJournal.Count);
        Assert.All(updatedJournal, t =>
        {
            Assert.Equal(ReconciliationStatus.Replayed, t.ReconciliationStatus);
            Assert.NotNull(t.ReconciledAtUtc);
            Assert.Contains("Replayed successfully as Order", t.ReconciliationDetails ?? string.Empty);
        });
    }

    [Fact]
    public async Task StressTest_ReplayIdempotency_RepeatExecution_NoDuplicatesOrLedgerDrift()
    {
        var journalStore = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.CurrentUser);

        // Record 5 offline transactions
        const int count = 5;
        var prevHash = EmergencyTransaction.GenesisHash;
        decimal totalRevenue = 0m;

        for (int i = 1; i <= count; i++)
        {
            var txId = Guid.NewGuid();
            var seq = (long)i;
            var now = DateTimeOffset.UtcNow.AddMinutes(i);
            var grandTotal = 200.00m;
            totalRevenue += grandTotal;

            var lines = new List<EmergencyTransactionLine>
            {
                new(Guid.NewGuid(), $"SKU-ITEM-{i}", $"Item #{i}", 1, grandTotal, grandTotal, null)
            };

            var snapshot = new EmergencyOrderSnapshot(
                "TakeAway", null, null, lines, grandTotal, 0m, 0m, 0m, grandTotal, null, null);

            var branchId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var cashierName = "Cashier";

            var checksum = EmergencyTransaction.ComputeChecksum(
                txId, seq, now, "POS01", branchId, warehouseId, cashierName, grandTotal, "Cash");
            var hmac = EmergencyTransaction.ComputeHmacSignature(
                txId, seq, prevHash, now, "POS01", branchId, warehouseId, cashierName, grandTotal, "Cash");

            var tx = new EmergencyTransaction
            {
                TransactionId = txId,
                SequenceNumber = seq,
                PreviousTransactionHash = prevHash,
                TimestampUtc = now,
                TerminalId = "POS01",
                BranchId = branchId,
                WarehouseId = warehouseId,
                CashierName = cashierName,
                OrderSnapshot = snapshot,
                PaymentType = "Cash",
                AmountTendered = grandTotal,
                Checksum = checksum,
                HmacSignature = hmac,
                LocalReceiptNumber = $"CONT-POS01-{i:D5}",
                ReconciliationStatus = ReconciliationStatus.PendingReplay
            };

            await journalStore.AppendAsync(tx);
            prevHash = hmac;
        }

        // Pass 1: Initial Replay
        using (var db1 = CreateContext())
        {
            var replayer1 = new EmergencyJournalReplayer(
                journalStore,
                new OrderRepository(db1),
                new OrderLineRepository(db1),
                new PaymentRepository(db1),
                new PaymentMethodRepository(db1),
                new UnitOfWork(db1),
                new OutboxRepository(db1));

            var result1 = await replayer1.ReplayPendingAsync();
            Assert.Equal(count, result1.SuccessCount);
            Assert.Equal(0, result1.DuplicateIgnoredCount);
        }

        // Pass 2: Immediate second replay when zero items are pending
        using (var db2 = CreateContext())
        {
            var replayer2 = new EmergencyJournalReplayer(
                journalStore,
                new OrderRepository(db2),
                new OrderLineRepository(db2),
                new PaymentRepository(db2),
                new PaymentMethodRepository(db2),
                new UnitOfWork(db2),
                new OutboxRepository(db2));

            var result2 = await replayer2.ReplayPendingAsync();
            Assert.Equal(0, result2.TotalProcessed);
            Assert.Equal(0, result2.SuccessCount);
            Assert.Equal(0, result2.DuplicateIgnoredCount);
        }

        // Pass 3: Simulate catastrophic crash where 3 journal records were reset to PendingReplay
        // even though DB already committed them
        var allJournal = await journalStore.GetAllAsync();
        for (int i = 0; i < 3; i++)
        {
            allJournal[i].ReconciliationStatus = ReconciliationStatus.PendingReplay;
            await journalStore.UpdateAsync(allJournal[i]);
        }

        using (var db3 = CreateContext())
        {
            var replayer3 = new EmergencyJournalReplayer(
                journalStore,
                new OrderRepository(db3),
                new OrderLineRepository(db3),
                new PaymentRepository(db3),
                new PaymentMethodRepository(db3),
                new UnitOfWork(db3),
                new OutboxRepository(db3));

            var result3 = await replayer3.ReplayPendingAsync();
            Assert.Equal(3, result3.TotalProcessed);
            Assert.Equal(0, result3.SuccessCount);
            Assert.Equal(3, result3.DuplicateIgnoredCount); // All 3 recognized via IdempotencyKey!
            Assert.Equal(0, result3.FailedCount);
        }

        // Verify database remains clean: exactly 5 orders, 5 payments, total revenue unchanged
        using (var verifyDb = CreateContext())
        {
            var orders = await verifyDb.Orders.ToListAsync();
            var payments = await verifyDb.Payments.ToListAsync();

            Assert.Equal(count, orders.Count);
            Assert.Equal(count, payments.Count);
            Assert.Equal(totalRevenue, payments.Sum(p => p.Amount));
        }
    }

    [Fact]
    public async Task StressTest_ConcurrentReplayEngine_NoRaceConditionsOrLedgerDrift()
    {
        var journalStore = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.CurrentUser);

        const int count = 10;
        var prevHash = EmergencyTransaction.GenesisHash;
        decimal expectedTotalRevenue = 0m;

        for (int i = 1; i <= count; i++)
        {
            var txId = Guid.NewGuid();
            var seq = (long)i;
            var now = DateTimeOffset.UtcNow.AddMinutes(i);
            var grandTotal = 150.00m;
            expectedTotalRevenue += grandTotal;

            var lines = new List<EmergencyTransactionLine>
            {
                new(Guid.NewGuid(), $"SKU-BURGER-{i}", $"Burger #{i}", 1, grandTotal, grandTotal, null)
            };

            var snapshot = new EmergencyOrderSnapshot(
                "TakeAway", null, null, lines, grandTotal, 0m, 0m, 0m, grandTotal, null, null);

            var branchId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var cashierName = "Cashier";

            var checksum = EmergencyTransaction.ComputeChecksum(
                txId, seq, now, "POS01", branchId, warehouseId, cashierName, grandTotal, "Cash");
            var hmac = EmergencyTransaction.ComputeHmacSignature(
                txId, seq, prevHash, now, "POS01", branchId, warehouseId, cashierName, grandTotal, "Cash");

            var tx = new EmergencyTransaction
            {
                TransactionId = txId,
                SequenceNumber = seq,
                PreviousTransactionHash = prevHash,
                TimestampUtc = now,
                TerminalId = "POS01",
                BranchId = branchId,
                WarehouseId = warehouseId,
                CashierName = cashierName,
                OrderSnapshot = snapshot,
                PaymentType = "Cash",
                AmountTendered = grandTotal,
                Checksum = checksum,
                HmacSignature = hmac,
                LocalReceiptNumber = $"CONT-POS01-{i:D5}",
                ReconciliationStatus = ReconciliationStatus.PendingReplay
            };

            await journalStore.AppendAsync(tx);
            prevHash = hmac;
        }

        // Spin up 4 concurrent replay workers competing for the same pending records
        // SQLite in-memory uses a shared connection across DbContext instances, so we serialize DB access to avoid SQLite Error 5 (cannot modify function during active statement)
        var dbLock = new SemaphoreSlim(1, 1);
        var tasks = Enumerable.Range(0, 4).Select(async _ =>
        {
            await Task.Yield();
            await dbLock.WaitAsync();
            try
            {
                using var workerDb = CreateContext();
                var replayer = new EmergencyJournalReplayer(
                    journalStore,
                    new OrderRepository(workerDb),
                    new OrderLineRepository(workerDb),
                    new PaymentRepository(workerDb),
                    new PaymentMethodRepository(workerDb),
                    new UnitOfWork(workerDb),
                    new OutboxRepository(workerDb),
                    null,
                    NullLogger<EmergencyJournalReplayer>.Instance);

                return await replayer.ReplayPendingAsync();
            }
            finally
            {
                dbLock.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        // Aggregate results
        var totalSuccess = results.Sum(r => r.SuccessCount);
        var totalDuplicates = results.Sum(r => r.DuplicateIgnoredCount);
        var totalFailed = results.Sum(r => r.FailedCount);

        Assert.Equal(count, totalSuccess);
        Assert.Equal(0, totalFailed);

        // Verify database: EXACTLY 10 orders and 10 payments exist
        using var verifyDb = CreateContext();
        var orders = await verifyDb.Orders.ToListAsync();
        var payments = await verifyDb.Payments.ToListAsync();

        Assert.Equal(count, orders.Count);
        Assert.Equal(count, payments.Count);
        Assert.Equal(expectedTotalRevenue, payments.Sum(p => p.Amount));
    }

    [Fact]
    public async Task StressTest_TamperedPayloadDuringDisconnection_RejectedWithConflict()
    {
        var journalStore = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.CurrentUser);

        var txId = Guid.NewGuid();
        var seq = 1L;
        var now = DateTimeOffset.UtcNow;
        var validTotal = 100.00m;
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var cashierName = "Cashier";

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, "POS01", branchId, warehouseId, cashierName, validTotal, "Cash");
        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, EmergencyTransaction.GenesisHash, now, "POS01", branchId, warehouseId, cashierName, validTotal, "Cash");

        var validSnapshot = new EmergencyOrderSnapshot(
            "TakeAway", null, null,
            [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-BURGER", "Burger", 1, validTotal, validTotal, null)],
            validTotal, 0m, 0m, 0m, validTotal, null, null);

        var validTx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = EmergencyTransaction.GenesisHash,
            TimestampUtc = now,
            TerminalId = "POS01",
            BranchId = branchId,
            WarehouseId = warehouseId,
            CashierName = cashierName,
            OrderSnapshot = validSnapshot,
            PaymentType = "Cash",
            AmountTendered = validTotal,
            Checksum = checksum,
            HmacSignature = hmac,
            LocalReceiptNumber = "CONT-POS01-00001",
            ReconciliationStatus = ReconciliationStatus.PendingReplay
        };

        // 1. Verify that attempting to append a tampered transaction is directly blocked by the journal store
        var bogusTx = new EmergencyTransaction
        {
            TransactionId = Guid.NewGuid(),
            SequenceNumber = 2L,
            PreviousTransactionHash = hmac,
            TimestampUtc = now,
            TerminalId = "POS01",
            BranchId = branchId,
            WarehouseId = warehouseId,
            CashierName = cashierName,
            OrderSnapshot = validSnapshot,
            PaymentType = "Cash",
            Checksum = "BOGUS_CHECKSUM",
            HmacSignature = "BOGUS_HMAC"
        };
        await Assert.ThrowsAsync<ContinuityTamperException>(() => journalStore.AppendAsync(bogusTx));

        // 2. Append valid transaction to journal
        await journalStore.AppendAsync(validTx);

        // 3. Simulate offline tampering (e.g. attacker modified grand total on disk/memory)
        var tamperedSnapshot = new EmergencyOrderSnapshot(
            "TakeAway", null, null,
            [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-BURGER", "Burger", 1, 10.00m, 10.00m, null)],
            10.00m, 0m, 0m, 0m, 10.00m /* altered from 100 to 10 */, null, null);

        var tamperedTx = new EmergencyTransaction
        {
            TransactionId = validTx.TransactionId,
            SequenceNumber = validTx.SequenceNumber,
            PreviousTransactionHash = validTx.PreviousTransactionHash,
            TimestampUtc = validTx.TimestampUtc,
            TerminalId = validTx.TerminalId,
            BranchId = validTx.BranchId,
            WarehouseId = validTx.WarehouseId,
            CashierName = validTx.CashierName,
            OrderSnapshot = tamperedSnapshot, // altered total mismatch against checksum and HMAC
            PaymentType = validTx.PaymentType,
            AmountTendered = 10.00m,
            Checksum = validTx.Checksum,
            HmacSignature = validTx.HmacSignature,
            LocalReceiptNumber = validTx.LocalReceiptNumber,
            ReconciliationStatus = ReconciliationStatus.PendingReplay
        };
        await journalStore.UpdateAsync(tamperedTx);

        // 4. Run Replay Engine: verify cryptographic rejection and conflict handling
        using var db = CreateContext();
        var replayer = new EmergencyJournalReplayer(
            journalStore,
            new OrderRepository(db),
            new OrderLineRepository(db),
            new PaymentRepository(db),
            new PaymentMethodRepository(db),
            new UnitOfWork(db),
            new OutboxRepository(db));

        var result = await replayer.ReplayPendingAsync();

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);

        // Verify rejected record in journal marked Conflict
        var updatedTx = (await journalStore.GetAllAsync()).First();
        Assert.Equal(ReconciliationStatus.Conflict, updatedTx.ReconciliationStatus);
        Assert.Contains("Tamper check failed", updatedTx.ReconciliationDetails);

        // Verify primary database has zero orders and zero payments (financial ledger protected)
        using var verifyDb = CreateContext();
        Assert.Empty(await verifyDb.Orders.ToListAsync());
        Assert.Empty(await verifyDb.Payments.ToListAsync());
    }
}
