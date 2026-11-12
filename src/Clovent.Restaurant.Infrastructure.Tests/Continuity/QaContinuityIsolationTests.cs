using Clovent.Platform.Configuration;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Infrastructure.Continuity;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Continuity;

[Collection("ContinuityIsolation")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class QaContinuityIsolationTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        CbosInstanceScope.ResetToProduction();
        foreach (var dir in _tempDirs)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
            }
        }
    }

    private string CreateTempRoot(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}");
        _tempDirs.Add(path);
        return path;
    }

    [Fact]
    public async Task ContinuityJournalAndOperationalCacheStores_InQaMode_IsolateStateInsideEachQaRoot()
    {
        var qaRootA = CreateTempRoot("cbos-qa-cont-a");
        var qaRootB = CreateTempRoot("cbos-qa-cont-b");
        const string connA = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_ContA;Trusted_Connection=True;";
        const string connB = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_ContB;Trusted_Connection=True;";

        var terminalGuid = Guid.NewGuid();
        var txId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, 1, now, "QA-T1", branchId, warehouseId, "QA Cashier", 100m, "Cash");
        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, 1, EmergencyTransaction.GenesisHash, now, "QA-T1", branchId, warehouseId, "QA Cashier", 100m, "Cash");

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRootA, connA))
        {
            var journal = new ProtectedContinuityJournalStore();
            await journal.AppendAsync(new EmergencyTransaction
            {
                TransactionId = txId,
                SequenceNumber = 1,
                PreviousTransactionHash = EmergencyTransaction.GenesisHash,
                TimestampUtc = now,
                TerminalId = "QA-T1",
                BranchId = branchId,
                WarehouseId = warehouseId,
                CashierId = Guid.NewGuid(),
                CashierName = "QA Cashier",
                OrderSnapshot = new EmergencyOrderSnapshot(
                    "Takeaway", null, null, [], 100m, 0, 0, 0, 100m, null, null),
                PaymentType = "Cash",
                AmountTendered = 100m,
                ChangeGiven = 0m,
                Checksum = checksum,
                HmacSignature = hmac
            });

            var cache = new ProtectedOperationalCacheStore();
            Assert.StartsWith(Path.GetFullPath(qaRootA), CbosInstanceScope.Current.OperationalCacheDirectory, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Path.Combine(qaRootA, "ContinuityJournal", "journal.dat")));
            Assert.True(Directory.Exists(Path.Combine(qaRootA, "OperationalCache")));
            Assert.Single(await journal.GetAllAsync());
            Assert.Null(await cache.LoadSnapshotAsync(terminalGuid));
        }

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRootB, connB))
        {
            var journalB = new ProtectedContinuityJournalStore();
            Assert.Empty(await journalB.GetAllAsync());
            Assert.False(File.Exists(Path.Combine(qaRootB, "ContinuityJournal", "journal.dat")));
        }
    }

    [Fact]
    public void ContinuityStores_InQaMode_RejectExplicitConstructorPathsAndJunctionsOutsideQaRoot()
    {
        var qaRoot = CreateTempRoot("cbos-qa-cont-escape");
        var outsideDir = CreateTempRoot("cbos-qa-cont-outside");
        const string conn = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_ContEscape;Trusted_Connection=True;";

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRoot, conn))
        {
            Assert.Throws<InvalidQaConfigurationException>(() =>
                new ProtectedContinuityJournalStore(Path.Combine(outsideDir, "journal.dat")));
            Assert.Throws<InvalidQaConfigurationException>(() =>
                new ProtectedOperationalCacheStore(outsideDir));
            Assert.False(Directory.Exists(outsideDir));
        }

        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(qaRoot);
            Directory.CreateDirectory(outsideDir);
            var linkPath = Path.Combine(qaRoot, "ContinuityJournal");
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /J \"{linkPath}\" \"{outsideDir}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            })!;
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);

            using (CbosInstanceScope.EnterQaScopeForTesting(qaRoot, conn))
            {
                Assert.Throws<InvalidQaConfigurationException>(() => new ProtectedContinuityJournalStore());
            }
        }
    }
}
