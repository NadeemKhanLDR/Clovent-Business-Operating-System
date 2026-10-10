using Clovent.Desktop.Printing;
using Clovent.Platform.Printing;
using Xunit;

namespace Clovent.Desktop.Tests.Printing;

public class PrinterManagementServiceTests : IDisposable
{
    private readonly string _tempFile;

    private sealed class FakeQueueProvider : IWindowsPrinterQueueProvider
    {
        public List<WindowsPrinterInfo> Installed { get; } =
        [
            new("POS-80", true, false, "Ready"),
            new("Kitchen-Thermal", false, true, "Ready")
        ];

        public IReadOnlyList<WindowsPrinterInfo> GetInstalledPrinters() => Installed;
        public bool IsPrinterInstalled(string printerName) => Installed.Any(p => p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));
        public string? GetDefaultPrinterName() => "POS-80";
        public PrinterHardwareStatusInfo GetPrinterStatus(string printerName) =>
            new(printerName, IsPrinterInstalled(printerName), true, PrinterHardwareCondition.Normal, 0, "Ready");
    }

    private sealed class FakePrinterAdapter : IPrinterAdapter
    {
        public PrinterConnectionType SupportedConnection => PrinterConnectionType.WindowsDriver;
        public bool ShouldFail { get; set; }
        public int DispatchCount { get; private set; }
        public PrinterJob? LastDispatchedJob { get; private set; }

        public Task<bool> IsAvailableAsync(PrinterProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(!ShouldFail);

        public Task<PrinterHealthSnapshot> CheckHealthAsync(PrinterProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(ShouldFail
                ? PrinterHealthSnapshot.Faulted(profile.SystemPrinterName ?? "TestPrinter", PrinterHardwareCondition.Offline, "Device offline", profile.Id)
                : PrinterHealthSnapshot.Healthy(profile.SystemPrinterName ?? "TestPrinter", profile.Id));

        public Task<PrintDispatchResult> DispatchAsync(PrinterProfile profile, PrinterJob job, CancellationToken cancellationToken = default)
        {
            DispatchCount++;
            LastDispatchedJob = job;

            if (ShouldFail)
            {
                return Task.FromResult(PrintDispatchResult.Failed(profile.SystemPrinterName, "Simulated device offline error."));
            }

            return Task.FromResult(PrintDispatchResult.SpoolerAccepted(profile.SystemPrinterName));
        }
    }

    public PrinterManagementServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"mgmt-test-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempFile)) File.Delete(_tempFile);
        }
        catch { }
    }

    [Fact]
    public async Task ProfileCrud_SavesAndRetrievesProfilesCorrectly()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter);

        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Front Counter",
            SystemPrinterName = "POS-80",
            Role = PrinterRole.Receipt,
            PaperWidth = PaperWidth.Width80mm,
            CharactersPerLine = 42
        };

        // Act & Assert 1: Save
        await service.SaveProfileAsync(profile);
        var config = await service.GetConfigurationAsync();
        Assert.Single(config.Profiles);
        Assert.Equal("Front Counter", config.Profiles[0].ProfileName);
        Assert.Equal(profile.Id, config.DefaultReceiptProfileId);

        // Act & Assert 2: Delete
        await service.DeleteProfileAsync(profile.Id);
        config = await service.GetConfigurationAsync();
        Assert.Empty(config.Profiles);
    }

    [Fact]
    public async Task DispatchReceipt_RoutesAndSubmitsJobSuccessfully()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter);

        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Receipt Printer",
            SystemPrinterName = "POS-80",
            Role = PrinterRole.Receipt,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        var receiptData = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-101",
            DailySalesNumber: 1,
            OrderType: "TakeAway",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new PrintableReceiptItem("Tea", "SKU-T", 1m, 50m, 50m)],
            Subtotal: 50m,
            TaxTotal: 0m,
            DiscountTotal: 0m,
            ServiceChargeTotal: 0m,
            GrandTotal: 50m,
            Balance: 0m,
            Payments: [new PrintableReceiptPayment("Cash", 50m)]);

        // Act
        var result = await service.DispatchReceiptAsync(receiptData, null, null, null);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, adapter.DispatchCount);
        Assert.NotNull(adapter.LastDispatchedJob);
        Assert.Equal(PrinterJobStatus.Submitted, tracker.GetJob(adapter.LastDispatchedJob.JobId)?.Status);
    }

    [Fact]
    public async Task DispatchReceipt_WhenAdapterFails_RecordsFailedStatusWithoutThrowing()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter { ShouldFail = true };
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter);

        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Receipt Printer",
            SystemPrinterName = "POS-80",
            Role = PrinterRole.Receipt,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        var receiptData = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-102",
            DailySalesNumber: 2,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [],
            Subtotal: 0m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 0m, Balance: 0m,
            Payments: []);

        // Act
        var result = await service.DispatchReceiptAsync(receiptData, null, null, null);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsQuarantined);
        Assert.Contains("offline", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        var recentJob = Assert.Single(tracker.GetRecentJobs(1));
        Assert.Equal(PrinterJobStatus.Quarantined, recentJob.Status);
    }

    [Fact]
    public async Task ExecuteTestPrint_RequiresExplicitAuthorization_ThrowsWhenUnauthorized()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter);

        var profileId = Guid.NewGuid();

        // Act & Assert: Must throw when authorizedExplicitly is false
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteTestPrintAsync(profileId, "Cashier", authorizedExplicitly: false));
    }

    [Fact]
    public async Task ExecuteTestPrint_WithExplicitAuthorization_DispatchesDiagnosticTestSlip()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter);

        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Diagnostic Printer",
            SystemPrinterName = "POS-80",
            Role = PrinterRole.Receipt,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        // Act
        var result = await service.ExecuteTestPrintAsync(profile.Id, "Cashier", authorizedExplicitly: true);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, adapter.DispatchCount);
        Assert.Contains("*** PRINTER DIAGNOSTIC TEST SLIP ***", adapter.LastDispatchedJob?.PayloadText);
        Assert.Equal("TestPrint", adapter.LastDispatchedJob?.DocumentType);
    }
}
