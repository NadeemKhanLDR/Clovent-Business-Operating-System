using Clovent.Restaurant.Continuity;
using Xunit;

namespace Clovent.Restaurant.Tests;

public sealed class ContinuityDomainTests
{
    [Fact]
    public void EmergencyTransaction_ValidChecksumAndHmac_VerifiesSuccessfully()
    {
        var txId = Guid.NewGuid();
        var seq = 1L;
        var prevHash = EmergencyTransaction.GenesisHash;
        var now = DateTimeOffset.UtcNow;
        var terminalId = "POS-01";
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var cashierName = "Alice";
        var grandTotal = 1250.50m;
        var paymentType = "Cash";

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, prevHash, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

        var snapshot = new EmergencyOrderSnapshot(
            "DineIn", null, null,
            [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-1", "Burger", 2, 500, 1000, null)],
            1000, 150, 0, 100.50m, grandTotal, null, null);

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
            PaymentType = paymentType,
            AmountTendered = 1500m,
            ChangeGiven = 249.50m,
            Checksum = checksum,
            HmacSignature = hmac
        };

        Assert.True(tx.VerifyChecksum());
        Assert.True(tx.VerifyHmacSignature());
    }

    [Fact]
    public void EmergencyTransaction_TamperedChecksum_FailsVerification()
    {
        var txId = Guid.NewGuid();
        var seq = 102L;
        var now = DateTimeOffset.UtcNow;
        var terminalId = "POS-01";
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var cashierName = "Bob";
        var grandTotal = 500m;
        var paymentType = "Cash";

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

        // Tamper grand total in snapshot
        var tamperedSnapshot = new EmergencyOrderSnapshot(
            "Takeaway", null, null, [], 500, 0, 0, 0, 400m /* altered */, null, null);

        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            TimestampUtc = now,
            TerminalId = terminalId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            CashierName = cashierName,
            OrderSnapshot = tamperedSnapshot,
            PaymentType = paymentType,
            Checksum = checksum
        };

        Assert.False(tx.VerifyChecksum());
    }

    [Fact]
    public void EmergencyTransaction_TamperedHmac_FailsVerification()
    {
        var txId = Guid.NewGuid();
        var seq = 2L;
        var prevHash = EmergencyTransaction.GenesisHash;
        var now = DateTimeOffset.UtcNow;
        var terminalId = "POS-01";
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var cashierName = "Charlie";
        var grandTotal = 750m;
        var paymentType = "Cash";

        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, prevHash, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

        var snapshot = new EmergencyOrderSnapshot(
            "Takeaway", null, null, [], 750, 0, 0, 0, 750m, null, null);

        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = prevHash,
            TimestampUtc = now,
            TerminalId = terminalId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            CashierName = cashierName,
            OrderSnapshot = snapshot,
            PaymentType = paymentType,
            HmacSignature = hmac + "TAMPERED"
        };

        Assert.False(tx.VerifyHmacSignature());
    }

    [Fact]
    public void ValidateChain_ValidSequentialTransactions_Succeeds()
    {
        var chain = CreateSampleChain(3);
        // Should execute without exception
        EmergencyTransaction.ValidateChain(chain);
    }

    [Fact]
    public void ValidateChain_DuplicateTransactionId_ThrowsContinuityTamperException()
    {
        var chain = CreateSampleChain(2);
        var duplicate = new EmergencyTransaction
        {
            TransactionId = chain[0].TransactionId, // duplicate
            SequenceNumber = 3,
            PreviousTransactionHash = chain[1].HmacSignature,
            TimestampUtc = DateTimeOffset.UtcNow,
            TerminalId = "POS-01",
            BranchId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            CashierName = "Cashier",
            PaymentType = "Cash",
            OrderSnapshot = new EmergencyOrderSnapshot("Takeaway", null, null, [], 100, 0, 0, 0, 100m, null, null)
        };

        var tamperedChain = new List<EmergencyTransaction> { chain[0], chain[1], duplicate };
        var ex = Assert.Throws<ContinuityTamperException>(() => EmergencyTransaction.ValidateChain(tamperedChain));
        Assert.Contains("Duplicate transaction identity", ex.Message);
    }

    [Fact]
    public void ValidateChain_ReorderedEntries_ThrowsContinuityTamperException()
    {
        var chain = CreateSampleChain(3);
        // Swap index 1 and 2
        var reordered = new List<EmergencyTransaction> { chain[0], chain[2], chain[1] };
        var ex = Assert.Throws<ContinuityTamperException>(() => EmergencyTransaction.ValidateChain(reordered));
        Assert.Contains("sequence violation", ex.Message);
    }

    [Fact]
    public void ValidateChain_TruncatedMiddleEntry_ThrowsContinuityTamperException()
    {
        var chain = CreateSampleChain(3);
        // Remove index 1: chain now has [0] (seq 1) and [2] (seq 3)
        var truncated = new List<EmergencyTransaction> { chain[0], chain[2] };
        var ex = Assert.Throws<ContinuityTamperException>(() => EmergencyTransaction.ValidateChain(truncated));
        Assert.Contains("sequence violation", ex.Message);
    }

    [Fact]
    public void ValidateChain_BrokenHashChain_ThrowsContinuityTamperException()
    {
        var chain = CreateSampleChain(2);
        var tx2 = chain[1];
        // Create broken tx2 with wrong previous hash
        var brokenTx2 = new EmergencyTransaction
        {
            TransactionId = tx2.TransactionId,
            SequenceNumber = tx2.SequenceNumber,
            PreviousTransactionHash = "BROKEN_PREVIOUS_HASH",
            TimestampUtc = tx2.TimestampUtc,
            TerminalId = tx2.TerminalId,
            BranchId = tx2.BranchId,
            WarehouseId = tx2.WarehouseId,
            CashierName = tx2.CashierName,
            OrderSnapshot = tx2.OrderSnapshot,
            PaymentType = tx2.PaymentType,
            Checksum = tx2.Checksum,
            HmacSignature = EmergencyTransaction.ComputeHmacSignature(
                tx2.TransactionId, tx2.SequenceNumber, "BROKEN_PREVIOUS_HASH", tx2.TimestampUtc,
                tx2.TerminalId, tx2.BranchId, tx2.WarehouseId, tx2.CashierName, tx2.OrderSnapshot.GrandTotal, tx2.PaymentType)
        };

        var brokenChain = new List<EmergencyTransaction> { chain[0], brokenTx2 };
        var ex = Assert.Throws<ContinuityTamperException>(() => EmergencyTransaction.ValidateChain(brokenChain));
        Assert.Contains("Broken hash chain", ex.Message);
    }

    private static List<EmergencyTransaction> CreateSampleChain(int count)
    {
        var list = new List<EmergencyTransaction>(count);
        var prevHash = EmergencyTransaction.GenesisHash;
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var terminalId = "POS-01";
        var cashierName = "Alice";

        for (int i = 1; i <= count; i++)
        {
            var txId = Guid.NewGuid();
            var seq = (long)i;
            var now = DateTimeOffset.UtcNow.AddMinutes(i);
            var grandTotal = 100m * i;
            var paymentType = "Cash";

            var checksum = EmergencyTransaction.ComputeChecksum(
                txId, seq, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

            var hmac = EmergencyTransaction.ComputeHmacSignature(
                txId, seq, prevHash, now, terminalId, branchId, warehouseId, cashierName, grandTotal, paymentType);

            var snapshot = new EmergencyOrderSnapshot(
                "Takeaway", null, null, [], grandTotal, 0, 0, 0, grandTotal, null, null);

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
                PaymentType = paymentType,
                AmountTendered = grandTotal,
                ChangeGiven = 0,
                Checksum = checksum,
                HmacSignature = hmac
            };

            list.Add(tx);
            prevHash = hmac;
        }

        return list;
    }
}
