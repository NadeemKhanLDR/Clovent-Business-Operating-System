using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Notifications;
using Clovent.Desktop.Printing;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Printing;
using Xunit;

namespace Clovent.Desktop.Tests.Printing;

public sealed class PeripheralCircuitBreakerAndQuarantineTests : IDisposable
{
    private readonly string _tempConfigFile;
    private readonly string _tempQuarantineDir;

    private sealed class FakeQueueProvider : IWindowsPrinterQueueProvider
    {
        public List<WindowsPrinterInfo> Installed { get; } =
        [
            new("POS-Thermal-80", true, false, "Ready"),
            new("Kitchen-Thermal", false, true, "Ready")
        ];

        public IReadOnlyList<WindowsPrinterInfo> GetInstalledPrinters() => Installed;
        public bool IsPrinterInstalled(string printerName) => Installed.Any(p => p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));
        public string? GetDefaultPrinterName() => "POS-Thermal-80";
        public PrinterHardwareStatusInfo GetPrinterStatus(string printerName) =>
            new(printerName, IsPrinterInstalled(printerName), true, PrinterHardwareCondition.Normal, 0, "Ready");
    }

    private sealed class ConfigurablePrinterAdapter : IPrinterAdapter
    {
        public PrinterConnectionType SupportedConnection => PrinterConnectionType.WindowsDriver;
        public PrinterHealthSnapshot NextHealthSnapshot { get; set; } =
            PrinterHealthSnapshot.Healthy("POS-Thermal-80");
        public bool ThrowOnHealthCheck { get; set; }
        public bool ThrowOnDispatch { get; set; }
        public bool DispatchShouldFail { get; set; }
        public int HealthCheckCount { get; private set; }
        public int DispatchCount { get; private set; }
        public PrinterJob? LastDispatchedJob { get; private set; }

        public Task<bool> IsAvailableAsync(PrinterProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(NextHealthSnapshot.IsReady);

        public Task<PrinterHealthSnapshot> CheckHealthAsync(PrinterProfile profile, CancellationToken cancellationToken = default)
        {
            HealthCheckCount++;
            if (ThrowOnHealthCheck)
            {
                throw new IOException("Simulated network timeout during peripheral status probe.");
            }
            return Task.FromResult(NextHealthSnapshot);
        }

        public Task<PrintDispatchResult> DispatchAsync(PrinterProfile profile, PrinterJob job, CancellationToken cancellationToken = default)
        {
            DispatchCount++;
            LastDispatchedJob = job;

            if (ThrowOnDispatch)
            {
                throw new IOException("Simulated USB disconnect during GDI spooler stream.");
            }

            if (DispatchShouldFail)
            {
                return Task.FromResult(PrintDispatchResult.Failed(profile.SystemPrinterName, "Windows spooler error: 0x00000005 Access Denied"));
            }

            return Task.FromResult(PrintDispatchResult.SpoolerAccepted(profile.SystemPrinterName));
        }
    }

    // Uses real in-memory NotificationService

    public PeripheralCircuitBreakerAndQuarantineTests()
    {
        _tempConfigFile = Path.Combine(Path.GetTempPath(), $"cbos-printer-cfg-{Guid.NewGuid():N}.json");
        _tempQuarantineDir = Path.Combine(Path.GetTempPath(), $"cbos-quarantine-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempConfigFile)) File.Delete(_tempConfigFile);
            if (Directory.Exists(_tempQuarantineDir)) Directory.Delete(_tempQuarantineDir, recursive: true);
        }
        catch { }
    }

    private PrintableReceiptData CreateSampleReceipt(string orderNumber = "ORD-TEST-100")
    {
        return new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: orderNumber,
            DailySalesNumber: 42,
            OrderType: "TakeAway",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new PrintableReceiptItem("Cappuccino", "COF-01", 1m, 120m, 120m)],
            Subtotal: 120m,
            TaxTotal: 6m,
            DiscountTotal: 0m,
            ServiceChargeTotal: 0m,
            GrandTotal: 126m,
            Balance: 0m,
            Payments: [new PrintableReceiptPayment("Cash", 126m)]);
    }

    [Fact]
    public async Task PreFlightCheck_DetectsPhysicalFault_QuarantinesJobWithoutCallingDispatch()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempConfigFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new ConfigurablePrinterAdapter();
        var quarantineStore = new PrintJobQuarantineStore(_tempQuarantineDir);
        var cbRegistry = new CircuitBreakerRegistry();
        var notifService = new NotificationService();

        var service = new PrinterManagementService(
            queueProvider, store, router, tracker, adapter,
            quarantineStore, cbRegistry, notifService);

        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Receipt Thermal",
            SystemPrinterName = "POS-Thermal-80",
            Role = PrinterRole.Receipt,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        // Simulate physical fault: Paper Out + Lid Open
        adapter.NextHealthSnapshot = PrinterHealthSnapshot.Faulted(
            "POS-Thermal-80",
            PrinterHardwareCondition.CoverOpen | PrinterHardwareCondition.PaperOut,
            "Lid open and roll empty",
            profile.Id);

        CashierReceiptFallbackNotification? fallbackEvent = null;
        service.CashierFallbackNotified += (s, e) => fallbackEvent = e;

        // Act
        var result = await service.DispatchReceiptAsync(CreateSampleReceipt("ORD-888"), null, null, null);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsQuarantined);
        Assert.Equal(0, adapter.DispatchCount); // Proactively caught before dispatching!
        Assert.Equal(1, adapter.HealthCheckCount);

        // Event fired for instantaneous cashier notification
        Assert.NotNull(fallbackEvent);
        Assert.Equal("POS-Thermal-80", fallbackEvent.PrinterName);
        Assert.Contains("CoverOpen", fallbackEvent.Reason);
        Assert.Contains("PaperOut", fallbackEvent.Reason);

        // Persisted to durable quarantine disk store
        var quarantinedJobs = await quarantineStore.GetQuarantinedJobsAsync();
        Assert.Single(quarantinedJobs);
        Assert.Equal(fallbackEvent.JobId, quarantinedJobs[0].JobId);
        Assert.Contains("Cappuccino", quarantinedJobs[0].PayloadText);

        // Tracked in job tracker as Quarantined
        var trackedJob = tracker.GetJob(fallbackEvent.JobId);
        Assert.NotNull(trackedJob);
        Assert.Equal(PrinterJobStatus.Quarantined, trackedJob.Status);

        // In-app notification added
        Assert.Single(notifService.Notifications);
        Assert.Contains("Receipt Printing Fallback", notifService.Notifications[0].Title);
    }

    [Fact]
    public async Task CircuitBreaker_TripsToOpenAfterConsecutiveFailures_FastFailsWithoutCallingAdapter()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempConfigFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new ConfigurablePrinterAdapter();
        var quarantineStore = new PrintJobQuarantineStore(_tempQuarantineDir);
        var cbRegistry = new CircuitBreakerRegistry();

        var service = new PrinterManagementService(
            queueProvider, store, router, tracker, adapter,
            quarantineStore, cbRegistry);

        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Receipt Thermal",
            SystemPrinterName = "POS-Thermal-80",
            Role = PrinterRole.Receipt,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        // Set adapter to throw (e.g. repeated USB disconnect)
        adapter.ThrowOnDispatch = true;

        // Act: Run 3 dispatches to trip breaker (Threshold = 3)
        for (int i = 0; i < 3; i++)
        {
            var res = await service.DispatchReceiptAsync(CreateSampleReceipt($"ORD-FAIL-{i}"), null, null, null);
            Assert.True(res.IsQuarantined);
        }

        var breaker = service.GetCircuitBreaker("POS-Thermal-80");
        Assert.Equal(CircuitBreakerState.Open, breaker.State);
        Assert.Equal(3, adapter.DispatchCount);

        // Act 4th dispatch: Breaker is OPEN -> must fast-fail immediately without calling CheckHealth or Dispatch
        int healthChecksBefore = adapter.HealthCheckCount;
        int dispatchesBefore = adapter.DispatchCount;

        var fastFailResult = await service.DispatchReceiptAsync(CreateSampleReceipt("ORD-FASTFAIL"), null, null, null);

        // Assert
        Assert.True(fastFailResult.IsQuarantined);
        Assert.Contains("circuit breaker is OPEN", fastFailResult.ErrorMessage);
        Assert.Equal(healthChecksBefore, adapter.HealthCheckCount); // Not called!
        Assert.Equal(dispatchesBefore, adapter.DispatchCount);     // Not called!

        // Total 4 quarantined jobs on disk
        Assert.Equal(4, await quarantineStore.GetQuarantinedJobCountAsync());

        // Resetting breaker restores execution
        service.ResetCircuitBreaker("POS-Thermal-80");
        Assert.Equal(CircuitBreakerState.Closed, breaker.State);
    }

    [Fact]
    public async Task ReplayQuarantinedJob_WhenPrinterRecovers_SuccessfullyDispatchesAndRemovesFromStore()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempConfigFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new ConfigurablePrinterAdapter();
        var quarantineStore = new PrintJobQuarantineStore(_tempQuarantineDir);
        var cbRegistry = new CircuitBreakerRegistry();

        var service = new PrinterManagementService(
            queueProvider, store, router, tracker, adapter,
            quarantineStore, cbRegistry);

        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Receipt Thermal",
            SystemPrinterName = "POS-Thermal-80",
            Role = PrinterRole.Receipt,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        // 1. Initially faulted -> job is quarantined
        adapter.NextHealthSnapshot = PrinterHealthSnapshot.Faulted(
            "POS-Thermal-80", PrinterHardwareCondition.CutterError, "Cutter blade locked", profile.Id);

        var dispatchRes = await service.DispatchReceiptAsync(CreateSampleReceipt("ORD-REPLAY-1"), null, null, null);
        Assert.True(dispatchRes.IsQuarantined);

        var quarantinedList = await quarantineStore.GetQuarantinedJobsAsync();
        var quarantinedJob = Assert.Single(quarantinedList);
        Assert.Equal(0, quarantinedJob.RetryCount);

        // 2. Hardware repaired! Next health check returns Healthy
        adapter.NextHealthSnapshot = PrinterHealthSnapshot.Healthy("POS-Thermal-80", profile.Id);

        // Act: Replay the quarantined job
        var replayResult = await service.ReplayQuarantinedJobAsync(quarantinedJob.JobId);

        // Assert
        Assert.True(replayResult.Success);
        Assert.Equal(1, adapter.DispatchCount);
        Assert.NotNull(adapter.LastDispatchedJob);
        Assert.Equal("CustomerReceipt", adapter.LastDispatchedJob.DocumentType);

        // Job removed from quarantine store
        Assert.Equal(0, await quarantineStore.GetQuarantinedJobCountAsync());
        Assert.Null(await quarantineStore.GetJobAsync(quarantinedJob.JobId));
    }

    [Fact]
    public async Task ReplayQuarantinedJob_WhenPrinterStillFaulted_IncrementsRetryCountAndKeepsInStore()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempConfigFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new ConfigurablePrinterAdapter();
        var quarantineStore = new PrintJobQuarantineStore(_tempQuarantineDir);

        var service = new PrinterManagementService(
            queueProvider, store, router, tracker, adapter,
            quarantineStore);

        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Receipt Thermal",
            SystemPrinterName = "POS-Thermal-80",
            Role = PrinterRole.Receipt,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        // Faulted
        adapter.NextHealthSnapshot = PrinterHealthSnapshot.Faulted(
            "POS-Thermal-80", PrinterHardwareCondition.PaperJam, "Paper jammed in feeder", profile.Id);

        await service.DispatchReceiptAsync(CreateSampleReceipt("ORD-JAM-1"), null, null, null);
        var quarantinedJob = (await quarantineStore.GetQuarantinedJobsAsync()).Single();

        // Act: Replay while still jammed
        var replayResult = await service.ReplayQuarantinedJobAsync(quarantinedJob.JobId);

        // Assert
        Assert.False(replayResult.Success);
        Assert.Contains("fault state", replayResult.ErrorMessage);

        // Still in quarantine store, but retry count incremented
        var updatedJob = await quarantineStore.GetJobAsync(quarantinedJob.JobId);
        Assert.NotNull(updatedJob);
        Assert.Equal(1, updatedJob.RetryCount);
        Assert.NotNull(updatedJob.LastRetryAtUtc);
        Assert.Contains("Paper jammed", updatedJob.LastRetryError);
    }

    [Fact]
    public async Task ReplayAllQuarantinedJobs_ProcessesBatchAndReturnsAccurateSummary()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempConfigFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new ConfigurablePrinterAdapter();
        var quarantineStore = new PrintJobQuarantineStore(_tempQuarantineDir);

        var service = new PrinterManagementService(
            queueProvider, store, router, tracker, adapter,
            quarantineStore);

        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Receipt Thermal",
            SystemPrinterName = "POS-Thermal-80",
            Role = PrinterRole.Receipt,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        // Quarantine 3 jobs
        adapter.NextHealthSnapshot = PrinterHealthSnapshot.Faulted(
            "POS-Thermal-80", PrinterHardwareCondition.Offline, "Unreachable", profile.Id);

        await service.DispatchReceiptAsync(CreateSampleReceipt("ORD-1"), null, null, null);
        await service.DispatchReceiptAsync(CreateSampleReceipt("ORD-2"), null, null, null);
        await service.DispatchReceiptAsync(CreateSampleReceipt("ORD-3"), null, null, null);

        Assert.Equal(3, await quarantineStore.GetQuarantinedJobCountAsync());

        // Now printer is back online
        adapter.NextHealthSnapshot = PrinterHealthSnapshot.Healthy("POS-Thermal-80", profile.Id);

        // Act
        var summary = await service.ReplayAllQuarantinedJobsAsync();

        // Assert
        Assert.Equal(3, summary.TotalAttempted);
        Assert.Equal(3, summary.SuccessfullyRecovered);
        Assert.Equal(0, summary.StillFailing);
        Assert.Equal(0, await quarantineStore.GetQuarantinedJobCountAsync());
    }
}
