using Clovent.Desktop.Printing;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
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
    }

    private sealed class FakePrinterAdapter : IPrinterAdapter
    {
        public PrinterConnectionType SupportedConnection => PrinterConnectionType.WindowsDriver;
        public bool ShouldFail { get; set; }
        public int DispatchCount { get; private set; }
        public PrinterJob? LastDispatchedJob { get; private set; }

        public Task<bool> IsAvailableAsync(PrinterProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(!ShouldFail);

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

    private sealed class FakeCurrentSession : ICurrentSession
    {
        public bool IsAuthenticated { get; set; } = true;
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Test Operator";
        public event EventHandler? Changed;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
    }

    private sealed class FakeAuthorizationService : IAuthorizationService
    {
        public HashSet<string> GrantedPermissions { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> GrantedRoles { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyCollection<string>)GrantedPermissions.ToList());

        public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(GrantedPermissions.Contains(permissionCode));

        public Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default) =>
            Task.FromResult(GrantedRoles.Contains(roleName));

        public Task<bool> SatisfiesPolicyAsync(Guid userId, string policyName, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
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
        Assert.Contains("offline", result.ErrorMessage);
        Assert.NotNull(adapter.LastDispatchedJob);
        Assert.Equal(PrinterJobStatus.Failed, tracker.GetJob(adapter.LastDispatchedJob.JobId)?.Status);
    }

    [Fact]
    public async Task ExecuteTestPrint_RequiresServiceBoundaryPermission_ThrowsWhenUnauthorized()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var session = new FakeCurrentSession();
        var authService = new FakeAuthorizationService(); // No permissions granted
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter, authService, session);

        var profileId = Guid.NewGuid();

        // Act & Assert: Must throw UnauthorizedAccessException at service boundary
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ExecuteTestPrintAsync(profileId, "Cashier"));
    }

    [Fact]
    public async Task ExecuteTestPrint_WithAuthorizedPermission_DispatchesDiagnosticTestSlip()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var session = new FakeCurrentSession();
        var authService = new FakeAuthorizationService();
        authService.GrantedPermissions.Add("feature.printers.test");
        authService.GrantedPermissions.Add("feature.printers.manage");
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter, authService, session);

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
        var result = await service.ExecuteTestPrintAsync(profile.Id, "Cashier");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, adapter.DispatchCount);
        Assert.Contains("*** PRINTER DIAGNOSTIC TEST SLIP ***", adapter.LastDispatchedJob?.PayloadText);
        Assert.Equal("TestPrint", adapter.LastDispatchedJob?.DocumentType);
    }

    [Fact]
    public async Task DispatchReceiptAsync_WhenJobAlreadySubmitted_SuppressesDuplicateSpoolerSubmission()
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

        var stableJobId = Guid.NewGuid();
        var receiptData = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-IDEMPOTENT-01",
            DailySalesNumber: 1,
            OrderType: "TakeAway",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [],
            Subtotal: 0m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 0m, Balance: 0m,
            Payments: []);

        // Act 1: Initial dispatch submits to spooler
        var firstResult = await service.DispatchReceiptAsync(receiptData, null, null, null, jobId: stableJobId);
        Assert.True(firstResult.Success);
        Assert.Equal(1, adapter.DispatchCount);

        // Act 2: Retry with exact same jobId
        var retryResult = await service.DispatchReceiptAsync(receiptData, null, null, null, jobId: stableJobId);

        // Assert: Spooler submission was suppressed; adapter dispatch count remains 1
        Assert.True(retryResult.Success);
        Assert.Equal(1, adapter.DispatchCount);
    }

    [Fact]
    public async Task DispatchReceiptAsync_WhenJobAmbiguous_ReturnsAmbiguousWithoutReSubmission()
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

        var stableJobId = Guid.NewGuid();
        var existingJob = PrinterJob.Create("CustomerReceipt", profile.Id, "POS-80", "Text", jobId: stableJobId);
        tracker.RecordJob(existingJob);
        tracker.MarkAmbiguous(stableJobId, "Spooler timeout");

        var receiptData = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-AMBIGUOUS-01",
            DailySalesNumber: 1,
            OrderType: "TakeAway",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [],
            Subtotal: 0m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 0m, Balance: 0m,
            Payments: []);

        // Act: Attempt dispatch on existing ambiguous job
        var result = await service.DispatchReceiptAsync(receiptData, null, null, null, jobId: stableJobId);

        // Assert: Duplicate submission is blocked; returns Ambiguous
        Assert.False(result.Success);
        Assert.Equal(PrinterJobStatus.Ambiguous, result.Status);
        Assert.Equal(0, adapter.DispatchCount);
    }
}
