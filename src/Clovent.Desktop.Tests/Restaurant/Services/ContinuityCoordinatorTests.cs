using Clovent.Desktop.Restaurant.Services;
using Clovent.Restaurant.Continuity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Services;

public sealed class ContinuityCoordinatorTests
{
    private sealed class FakeJournalStore : IContinuityJournalStore
    {
        public List<EmergencyTransaction> StoredTransactions { get; } = new();

        public Task AppendAsync(EmergencyTransaction transaction, CancellationToken cancellationToken = default)
        {
            StoredTransactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EmergencyTransaction>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmergencyTransaction>>(StoredTransactions);

        public Task<IReadOnlyList<EmergencyTransaction>> GetPendingReplayAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmergencyTransaction>>(
                StoredTransactions.Where(t => t.ReconciliationStatus == ReconciliationStatus.PendingReplay).ToList());

        public Task<string> GetLastTransactionHashAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(StoredTransactions.LastOrDefault()?.HmacSignature ?? EmergencyTransaction.GenesisHash);

        public Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((long)StoredTransactions.Count + 1);

        public Task UpdateAsync(EmergencyTransaction transaction, CancellationToken cancellationToken = default)
        {
            var idx = StoredTransactions.FindIndex(t => t.TransactionId == transaction.TransactionId);
            if (idx >= 0)
            {
                StoredTransactions[idx] = transaction;
            }
            return Task.CompletedTask;
        }

        public Task<ContinuityJournalStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new ContinuityJournalStatistics(
                TotalRecorded: StoredTransactions.Count,
                PendingReplayCount: StoredTransactions.Count(t => t.ReconciliationStatus == ReconciliationStatus.PendingReplay),
                ReplayedCount: StoredTransactions.Count(t => t.ReconciliationStatus == ReconciliationStatus.Replayed),
                FailedOrConflictCount: StoredTransactions.Count(t => t.ReconciliationStatus is ReconciliationStatus.Conflict or ReconciliationStatus.Failed),
                OldestPendingAtUtc: null);
            return Task.FromResult(stats);
        }
    }

    private sealed class FakeCacheStore : IOperationalCacheStore
    {
        public OperationalCacheSnapshot? CurrentSnapshot { get; set; }

        public Task SaveSnapshotAsync(OperationalCacheSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            CurrentSnapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<OperationalCacheSnapshot?> LoadSnapshotAsync(Guid terminalId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentSnapshot);

        public Task<OperationalCacheMetadata?> GetMetadataAsync(Guid terminalId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentSnapshot?.Metadata);

        public Task<bool> HasValidCacheAsync(Guid branchId, Guid terminalId, CacheFreshnessPolicy policy, CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentSnapshot != null && CurrentSnapshot.Metadata.TerminalId == terminalId);
    }

    [Fact]
    public void SetTerminalContext_UpdatesPropertiesCorrectly()
    {
        var journalStore = new FakeJournalStore();
        var cacheStore = new FakeCacheStore();
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

        var coordinator = new ContinuityCoordinator(journalStore, cacheStore, scopeFactory);

        var terminalId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        coordinator.SetTerminalContext(
            companyId, "Acme Corp",
            branchId, "Downtown Branch",
            terminalId, "POS Terminal 1", "POS01",
            warehouseId, "Kitchen Store",
            "PKR", "Rs.", 2);

        Assert.Equal(terminalId, coordinator.CurrentTerminalId);
        Assert.Equal(branchId, coordinator.CurrentBranchId);
        Assert.Equal("POS01", coordinator.CurrentTerminalCode);
    }

    [Fact]
    public void GetNextLocalReceiptNumber_GeneratesSequentialCollisionFreeNumbers()
    {
        var journalStore = new FakeJournalStore();
        var cacheStore = new FakeCacheStore();
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

        var coordinator = new ContinuityCoordinator(journalStore, cacheStore, scopeFactory);
        coordinator.SetTerminalContext(
            Guid.NewGuid(), "Co", Guid.NewGuid(), "Br",
            Guid.NewGuid(), "Terminal", "POS01",
            Guid.NewGuid(), "Wh", "PKR", "Rs.", 2);

        var num1 = coordinator.GetNextLocalReceiptNumber(1);
        var num2 = coordinator.GetNextLocalReceiptNumber(2);
        var num3 = coordinator.GetNextLocalReceiptNumber(100);

        Assert.Equal("CONT-POS01-00001", num1);
        Assert.Equal("CONT-POS01-00002", num2);
        Assert.Equal("CONT-POS01-00100", num3);
    }

    [Fact]
    public void EnterAndExitContinuityMode_TogglesStateAndFiresEvent()
    {
        var journalStore = new FakeJournalStore();
        var cacheStore = new FakeCacheStore();
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

        var coordinator = new ContinuityCoordinator(journalStore, cacheStore, scopeFactory);

        var eventFiredCount = 0;
        ContinuityStateChangedEventArgs? lastArgs = null;
        coordinator.StateChanged += (_, args) =>
        {
            eventFiredCount++;
            lastArgs = args;
        };

        Assert.False(coordinator.IsContinuityModeActive);

        // Enter Continuity Mode
        coordinator.EnterContinuityMode("Simulated DB connection timeout");

        Assert.True(coordinator.IsContinuityModeActive);
        Assert.Equal("Simulated DB connection timeout", coordinator.ContinuityReason);
        Assert.NotNull(coordinator.ContinuityEnteredAtUtc);
        Assert.Equal(1, eventFiredCount);
        Assert.NotNull(lastArgs);
        Assert.True(lastArgs!.IsActive);

        // Exit Continuity Mode
        coordinator.ExitContinuityMode(replayedCount: 3);

        Assert.False(coordinator.IsContinuityModeActive);
        Assert.Null(coordinator.ContinuityReason);
        Assert.Equal(2, eventFiredCount);
        Assert.False(lastArgs.IsActive);
        Assert.Equal(3, lastArgs.ReplayedCount);
    }

    [Fact]
    public async Task EnsureCacheLoadedAsync_WhenStoreHasValidSnapshot_LoadsInMemory()
    {
        var terminalId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var categories = new List<CachedCategory>
        {
            new(Guid.NewGuid(), "Burgers", null, null, 1, "Active")
        };

        var payload = new OperationalCachePayload
        {
            Categories = categories,
            Products = [],
            Variants = [],
            QuickOrderTemplates = [],
            DiningAreas = [],
            Tables = [],
            Discounts = [],
            PaymentMethods = [],
            Operators = [],
            Customers = []
        };

        var now = DateTimeOffset.UtcNow;
        var checksum = OperationalCacheSnapshot.ComputePayloadChecksum(payload);
        var hmac = OperationalCacheSnapshot.ComputeHmacSignature(
            1, "1.2.0", now, companyId, branchId, terminalId, warehouseId, checksum);

        var metadata = new OperationalCacheMetadata(
            SchemaVersion: 1,
            CacheVersion: "1.2.0",
            GeneratedAtUtc: now,
            LastSuccessfulSyncUtc: now,
            CompanyId: companyId,
            CompanyName: "Burger House",
            BranchId: branchId,
            BranchName: "Main Branch",
            TerminalId: terminalId,
            TerminalName: "Counter 1",
            TerminalCode: "POS01",
            WarehouseId: warehouseId,
            WarehouseName: "Kitchen",
            CurrencyCode: "PKR",
            CurrencySymbol: "Rs.",
            CurrencyDecimalPlaces: 2,
            SourceDatabaseIdentity: "TestDB",
            SourceRevision: "REV-1",
            TotalCategories: 1,
            TotalProducts: 0,
            TotalVariants: 0,
            TotalTemplates: 0,
            PayloadChecksum: checksum,
            HmacSignature: hmac);

        var snapshot = new OperationalCacheSnapshot(metadata, payload);

        var cacheStore = new FakeCacheStore { CurrentSnapshot = snapshot };
        var journalStore = new FakeJournalStore();
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

        var coordinator = new ContinuityCoordinator(journalStore, cacheStore, scopeFactory);
        coordinator.SetTerminalContext(
            companyId, "Burger House",
            branchId, "Main Branch",
            terminalId, "Counter 1", "POS01",
            warehouseId, "Kitchen", "PKR", "Rs.", 2);

        var loaded = await coordinator.EnsureCacheLoadedAsync();

        Assert.NotNull(loaded);
        Assert.NotNull(coordinator.ActiveCache);
        Assert.Equal(CacheValidationStatus.Valid, coordinator.CacheStatus);
        Assert.Single(coordinator.ActiveCache!.Payload.Categories);

        var (status, _) = await coordinator.ValidateCacheAsync();
        Assert.Equal(CacheValidationStatus.Valid, status);
    }

    [Fact]
    public void EnterAndExitContinuityMode_LogsToSmartPosLayoutTelemetryAndDiagnosticFile()
    {
        var journalStore = new FakeJournalStore();
        var cacheStore = new FakeCacheStore();
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

        var coordinator = new ContinuityCoordinator(journalStore, cacheStore, scopeFactory);
        var terminalId = Guid.NewGuid();
        coordinator.SetTerminalContext(
            Guid.NewGuid(), "Co", Guid.NewGuid(), "Br",
            terminalId, "Terminal", "POS01",
            Guid.NewGuid(), "Wh", "PKR", "Rs.", 2);

        var reason = "Database connection timed out during checkout test";
        coordinator.EnterContinuityMode(reason);

        var telemetryPath = Path.Combine(Clovent.Desktop.Restaurant.SmartPos.SmartPosLayoutTelemetry.TelemetryDir, "continuity_events_REAL_runtime.txt");
        Assert.True(File.Exists(telemetryPath));
        var content = File.ReadAllText(telemetryPath);
        Assert.Contains("CONTINUITY_EVENT: EnterContinuityMode", content);
        Assert.Contains(reason, content);
        Assert.Contains("Code='POS01'", content);

        coordinator.ExitContinuityMode(replayedCount: 7);

        content = File.ReadAllText(telemetryPath);
        Assert.Contains("CONTINUITY_EVENT: ExitContinuityMode", content);
        Assert.Contains("ReplayedCount: 7", content);
    }

    [Fact]
    public async Task RecordEmergencySale_LogsToSmartPosLayoutTelemetry()
    {
        var journalStore = new FakeJournalStore();
        var cacheStore = new FakeCacheStore();
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

        var coordinator = new ContinuityCoordinator(journalStore, cacheStore, scopeFactory);
        coordinator.SetTerminalContext(
            Guid.NewGuid(), "Co", Guid.NewGuid(), "Br",
            Guid.NewGuid(), "Terminal", "POS01",
            Guid.NewGuid(), "Wh", "PKR", "Rs.", 2);

        var txId = Guid.NewGuid();
        var tx = new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = 1,
            LocalReceiptNumber = "CONT-POS01-00001",
            OrderSnapshot = new EmergencyOrderSnapshot(
                "Takeaway", null, null, [], 100m, 0, 0, 0, 100m, null, null),
            PaymentType = "Cash"
        };

        await coordinator.RecordEmergencySaleAsync(tx);

        var telemetryPath = Path.Combine(Clovent.Desktop.Restaurant.SmartPos.SmartPosLayoutTelemetry.TelemetryDir, "continuity_events_REAL_runtime.txt");
        Assert.True(File.Exists(telemetryPath));
        var content = File.ReadAllText(telemetryPath);
        Assert.Contains("CONTINUITY_EVENT: RecordEmergencySale", content);
        Assert.Contains("CONT-POS01-00001", content);
    }
}
