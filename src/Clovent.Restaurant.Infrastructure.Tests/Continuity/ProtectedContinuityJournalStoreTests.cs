using System.Security.Cryptography;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Infrastructure.Continuity;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Continuity;

public sealed class ProtectedContinuityJournalStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;

    public ProtectedContinuityJournalStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CloventTest_Continuity_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "journal.dat");
    }

    public void Dispose()
    {
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
    public async Task AppendAndGetAllAsync_WithLocalMachineScope_RoundTripsSuccessfully()
    {
        var store = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.LocalMachine);
        Assert.Equal(DataProtectionScope.LocalMachine, store.ProtectionScope);

        var tx = CreateTestTransaction(1, EmergencyTransaction.GenesisHash);
        await store.AppendAsync(tx);

        var retrieved = await store.GetAllAsync();
        Assert.Single(retrieved);
        Assert.Equal(tx.TransactionId, retrieved[0].TransactionId);
        Assert.Equal(tx.OrderSnapshot.GrandTotal, retrieved[0].OrderSnapshot.GrandTotal);
        Assert.Equal(tx.HmacSignature, retrieved[0].HmacSignature);
    }

    [Fact]
    public async Task AppendAndGetAllAsync_WithCurrentUserScope_RoundTripsSuccessfully()
    {
        var store = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.CurrentUser);
        Assert.Equal(DataProtectionScope.CurrentUser, store.ProtectionScope);

        var tx = CreateTestTransaction(1, EmergencyTransaction.GenesisHash);
        await store.AppendAsync(tx);

        var retrieved = await store.GetAllAsync();
        Assert.Single(retrieved);
        Assert.Equal(tx.TransactionId, retrieved[0].TransactionId);
    }

    [Fact]
    public async Task AppendAsync_EnforcesHashChaining()
    {
        var store = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.LocalMachine);

        var tx1 = CreateTestTransaction(1, EmergencyTransaction.GenesisHash);
        await store.AppendAsync(tx1);

        var tx2 = CreateTestTransaction(2, tx1.HmacSignature);
        await store.AppendAsync(tx2);

        var all = await store.GetAllAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal(tx1.HmacSignature, all[1].PreviousTransactionHash);
    }

    [Fact]
    public async Task AppendAsync_BrokenHashChain_ThrowsContinuityTamperException()
    {
        var store = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.LocalMachine);

        var tx1 = CreateTestTransaction(1, EmergencyTransaction.GenesisHash);
        await store.AppendAsync(tx1);

        // tx2 provides bogus predecessor hash
        var brokenTx2 = CreateTestTransaction(2, "BOGUS_PREVIOUS_HASH");
        var ex = await Assert.ThrowsAsync<ContinuityTamperException>(() => store.AppendAsync(brokenTx2));
        Assert.Contains("Broken hash chain", ex.Message);
    }

    [Fact]
    public async Task AppendAsync_SequenceGap_ThrowsContinuityTamperException()
    {
        var store = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.LocalMachine);

        var tx1 = CreateTestTransaction(1, EmergencyTransaction.GenesisHash);
        await store.AppendAsync(tx1);

        // Sequence number jumps to 3 instead of 2
        var gapTx = CreateTestTransaction(3, tx1.HmacSignature);
        var ex = await Assert.ThrowsAsync<ContinuityTamperException>(() => store.AppendAsync(gapTx));
        Assert.Contains("Sequence gap", ex.Message);
    }

    [Fact]
    public async Task AppendAsync_TamperedHmac_ThrowsContinuityTamperException()
    {
        var store = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.LocalMachine);

        var tx = CreateTestTransaction(1, EmergencyTransaction.GenesisHash);
        var tamperedTx = new EmergencyTransaction
        {
            TransactionId = tx.TransactionId,
            SequenceNumber = tx.SequenceNumber,
            PreviousTransactionHash = tx.PreviousTransactionHash,
            TimestampUtc = tx.TimestampUtc,
            TerminalId = tx.TerminalId,
            BranchId = tx.BranchId,
            WarehouseId = tx.WarehouseId,
            CashierId = tx.CashierId,
            CashierName = tx.CashierName,
            OrderSnapshot = tx.OrderSnapshot,
            PaymentType = tx.PaymentType,
            AmountTendered = tx.AmountTendered,
            ChangeGiven = tx.ChangeGiven,
            Checksum = tx.Checksum,
            HmacSignature = tx.HmacSignature + "CORRUPT"
        };

        var ex = await Assert.ThrowsAsync<ContinuityTamperException>(() => store.AppendAsync(tamperedTx));
        Assert.Contains("HMAC signature validation failed", ex.Message);
    }

    [Fact]
    public async Task CorruptedJournalFile_ThrowsContinuitySecurityException_WithoutSilentFallback()
    {
        var store = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.LocalMachine);

        var tx = CreateTestTransaction(1, EmergencyTransaction.GenesisHash);
        await store.AppendAsync(tx);

        // Corrupt encrypted ciphertext on disk
        await File.WriteAllTextAsync(_journalPath, "NotAValidBase64OrEncryptedBlob");

        var ex = await Assert.ThrowsAsync<ContinuitySecurityException>(() => store.GetAllAsync());
        Assert.Contains("Cryptographic decryption of emergency journal failed", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesReconciliationStatusSuccessfully()
    {
        var store = new ProtectedContinuityJournalStore(_journalPath, DataProtectionScope.LocalMachine);

        var tx = CreateTestTransaction(1, EmergencyTransaction.GenesisHash);
        await store.AppendAsync(tx);

        tx.ReconciliationStatus = ReconciliationStatus.Replayed;
        tx.ReconciledAtUtc = DateTimeOffset.UtcNow;
        tx.ReconciliationDetails = "Successfully replayed to primary database.";

        await store.UpdateAsync(tx);

        var stats = await store.GetStatisticsAsync();
        Assert.Equal(1, stats.TotalRecorded);
        Assert.Equal(0, stats.PendingReplayCount);
        Assert.Equal(1, stats.ReplayedCount);
    }

    private static EmergencyTransaction CreateTestTransaction(long seq, string prevHash)
    {
        var txId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var terminalId = "POS-01";
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var cashierName = "Alice";
        var grandTotal = 150.00m;
        var paymentType = "Cash";

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, prevHash, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

        var snapshot = new EmergencyOrderSnapshot(
            "Takeaway", null, null, [], grandTotal, 0, 0, 0, grandTotal, null, null);

        return new EmergencyTransaction
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
            PaymentType = paymentType,
            AmountTendered = grandTotal,
            ChangeGiven = 0,
            Checksum = checksum,
            HmacSignature = hmac
        };
    }
}
