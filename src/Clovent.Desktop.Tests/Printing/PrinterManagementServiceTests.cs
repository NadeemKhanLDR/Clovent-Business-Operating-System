using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Printing;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Platform.Printing;
using Xunit;

namespace Clovent.Desktop.Tests.Printing;

public class PrinterManagementServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly string _tempPosSettingsFile;

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
        public PrinterProfile? LastDispatchedProfile { get; private set; }
        public Action<PrinterJob>? OnDispatchAction { get; set; }
        public int DispatchDelayMs { get; set; }

        public Task<bool> IsAvailableAsync(PrinterProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(!ShouldFail);

        public Task<PrinterHealthSnapshot> CheckHealthAsync(PrinterProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(ShouldFail
                ? PrinterHealthSnapshot.Faulted(profile.SystemPrinterName ?? "TestPrinter", PrinterHardwareCondition.Offline, "Device offline", profile.Id)
                : PrinterHealthSnapshot.Healthy(profile.SystemPrinterName ?? "TestPrinter", profile.Id));

        public async Task<PrintDispatchResult> DispatchAsync(PrinterProfile profile, PrinterJob job, CancellationToken cancellationToken = default)
        {
            DispatchCount++;
            LastDispatchedJob = job;
            LastDispatchedProfile = profile;
            OnDispatchAction?.Invoke(job);

            if (DispatchDelayMs > 0)
            {
                await Task.Delay(DispatchDelayMs, cancellationToken).ConfigureAwait(false);
            }

            if (ShouldFail)
            {
                return PrintDispatchResult.Failed(profile.SystemPrinterName, "Simulated device offline error.");
            }

            return PrintDispatchResult.SpoolerAccepted(profile.SystemPrinterName);
        }
    }

    private sealed class FakeCurrentSession : ICurrentSession
    {
        public bool IsAuthenticated { get; set; } = true;
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Test Operator";
        public event EventHandler? Changed { add { } remove { } }
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
        _tempPosSettingsFile = Path.Combine(Path.GetTempPath(), $"pos-settings-mgmt-test-{Guid.NewGuid():N}.json");
        PosSettingsStore.SetTestingOverrides(_tempPosSettingsFile);
    }

    public void Dispose()
    {
        PosSettingsStore.ResetTestingOverrides();
        try
        {
            if (File.Exists(_tempFile)) File.Delete(_tempFile);
            if (File.Exists(_tempPosSettingsFile)) File.Delete(_tempPosSettingsFile);
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

    [Fact]
    public async Task DispatchReceiptAsync_MarksInFlightBeforeAdapterDispatch()
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
            OrderNumber: "ORD-INFLIGHT-TEST",
            DailySalesNumber: 1,
            OrderType: "TakeAway",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new PrintableReceiptItem("Tea", "SKU-T", 1m, 50m, 50m)],
            Subtotal: 50m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 50m, Balance: 0m,
            Payments: [new PrintableReceiptPayment("Cash", 50m)]);

        PrinterJobStatus? statusDuringDispatch = null;
        adapter.OnDispatchAction = job =>
        {
            statusDuringDispatch = tracker.GetJob(job.JobId)?.Status;
        };

        // Act
        var result = await service.DispatchReceiptAsync(receiptData, null, null, null);

        // Assert: Job was marked InFlight BEFORE spooler adapter dispatch was executed
        Assert.True(result.Success);
        Assert.Equal(PrinterJobStatus.InFlight, statusDuringDispatch);
        // After dispatch completion, tracker transitioned to Submitted
        Assert.Equal(PrinterJobStatus.Submitted, tracker.GetJob(adapter.LastDispatchedJob!.JobId)?.Status);
    }

    [Fact]
    public async Task DispatchReceiptAsync_ConcurrentDispatchesSameJob_SuppressesDuplicateAndDispatchesOnlyOnce()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter { DispatchDelayMs = 150 };
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
            OrderNumber: "ORD-CONCURRENT-01",
            DailySalesNumber: 1,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new PrintableReceiptItem("Burger", "SKU-B", 1m, 200m, 200m)],
            Subtotal: 200m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 200m, Balance: 0m,
            Payments: [new PrintableReceiptPayment("Cash", 200m)]);

        // Act: Run two concurrent dispatches for the exact same jobId simultaneously
        var task1 = service.DispatchReceiptAsync(receiptData, null, null, null, jobId: stableJobId);
        var task2 = service.DispatchReceiptAsync(receiptData, null, null, null, jobId: stableJobId);

        var results = await Task.WhenAll(task1, task2);

        // Assert: Only 1 dispatch reached the spooler adapter; duplicate was suppressed
        Assert.Equal(1, adapter.DispatchCount);
        Assert.Contains(results, r => r.Success);
        Assert.Contains(results, r => !r.Success && r.Status == PrinterJobStatus.Ambiguous);
    }

    [Fact]
    public async Task EnsureScopeOwnership_ThrowsWhenTerminalMismatch_EvenForAdmin()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var auth = new FakeAuthorizationService();
        auth.GrantedRoles.Add("Administrator"); // Admin role
        var session = new FakeCurrentSession();
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter, auth, session);

        var activeTerminalId = Guid.NewGuid();
        var differentTerminalId = Guid.NewGuid();
        PosSettingsStore.SaveTerminalId(activeTerminalId);

        var settings = new TerminalReceiptSettings { TerminalId = differentTerminalId };

        // Act & Assert 1: Saving settings for a different terminal throws UnauthorizedAccessException
        var ex1 = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SaveTerminalReceiptSettingsAsync(differentTerminalId, settings));
        Assert.Contains("Scope ownership mismatch for terminal", ex1.Message);
        Assert.Contains("Manager/Admin status cannot bypass ownership boundaries", ex1.Message);

        // Act & Assert 2: Dispatching receipt for a different terminal throws UnauthorizedAccessException
        var receiptData = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-SCOPE-01",
            DailySalesNumber: 1,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new PrintableReceiptItem("Burger", "SKU-B", 1m, 200m, 200m)],
            Subtotal: 200m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 200m, Balance: 0m,
            Payments: [new PrintableReceiptPayment("Cash", 200m)]);

        var ex2 = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DispatchReceiptAsync(receiptData, terminalId: differentTerminalId));
        Assert.Contains("Scope ownership mismatch for terminal", ex2.Message);
        Assert.Contains("Manager/Admin status cannot bypass ownership boundaries", ex2.Message);
    }

    [Fact]
    public async Task EnsureScopeOwnership_ThrowsWhenBranchMismatch()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter);

        var activeBranchId = Guid.NewGuid();
        var differentBranchId = Guid.NewGuid();
        PosSettingsStore.SaveBranchId(activeBranchId);

        // Act & Assert: Dispatching text for a different branch throws UnauthorizedAccessException
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DispatchTextAsync("Test text", branchId: differentBranchId));
        Assert.Contains("Scope ownership mismatch for branch", ex.Message);
        Assert.Contains("Manager/Admin status cannot bypass ownership boundaries", ex.Message);
    }

    [Fact]
    public async Task EnsureScopeOwnership_SucceedsWhenScopeMatches()
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

        var termId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        PosSettingsStore.SaveTerminalId(termId);
        PosSettingsStore.SaveBranchId(branchId);

        var receiptData = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-MATCH-01",
            DailySalesNumber: 1,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new PrintableReceiptItem("Burger", "SKU-B", 1m, 200m, 200m)],
            Subtotal: 200m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 200m, Balance: 0m,
            Payments: [new PrintableReceiptPayment("Cash", 200m)]);

        // Act: Scope matches active workstation
        var result = await service.DispatchReceiptAsync(receiptData, branchId: branchId, terminalId: termId);

        // Assert: Succeeded without error
        Assert.True(result.Success);
        Assert.Equal(1, adapter.DispatchCount);
    }

    [Fact]
    public async Task CashDrawer_NotTriggered_OnReprints()
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
            ProfileName = "Cash Drawer Receipt Printer",
            SystemPrinterName = "POS-80",
            Role = PrinterRole.Receipt,
            SupportsCashDrawer = true, // Drawer enabled on profile
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        var receiptData = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-REPRINT-01",
            DailySalesNumber: 1,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new PrintableReceiptItem("Burger", "SKU-B", 1m, 200m, 200m)],
            Subtotal: 200m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 200m, Balance: 0m,
            Payments: [new PrintableReceiptPayment("Cash", 200m)]);

        // Act: Dispatch as reprint
        var result = await service.DispatchReceiptAsync(
            receiptData,
            isReprint: true,
            reprintCount: 1,
            reprintReason: "Customer requested reprint",
            copyType: ReceiptCopyType.Reprint);

        // Assert: Cash drawer kick MUST be suppressed on reprints
        Assert.True(result.Success);
        Assert.NotNull(adapter.LastDispatchedProfile);
        Assert.False(adapter.LastDispatchedProfile.SupportsCashDrawer, "Cash drawer must NOT kick on reprints.");
    }

    [Fact]
    public async Task CashDrawer_NotTriggered_OnAdditionalCopies()
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
            ProfileName = "Cash Drawer Receipt Printer",
            SystemPrinterName = "POS-80",
            Role = PrinterRole.Receipt,
            SupportsCashDrawer = true,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        var receiptData = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-ADDITIONAL-01",
            DailySalesNumber: 1,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new PrintableReceiptItem("Burger", "SKU-B", 1m, 200m, 200m)],
            Subtotal: 200m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 200m, Balance: 0m,
            Payments: [new PrintableReceiptPayment("Cash", 200m)]);

        // Act: Dispatch as copy 2 (isAdditionalCopy = true)
        var result = await service.DispatchReceiptAsync(
            receiptData,
            isReprint: false,
            isAdditionalCopy: true,
            copyType: ReceiptCopyType.Customer);

        // Assert: Cash drawer kick MUST be suppressed on additional copies (copy 2..N)
        Assert.True(result.Success);
        Assert.NotNull(adapter.LastDispatchedProfile);
        Assert.False(adapter.LastDispatchedProfile.SupportsCashDrawer, "Cash drawer must NOT kick on copy 2..N.");
    }

    [Fact]
    public async Task CashDrawer_NotTriggered_WhenPaymentMethodHasNoCash()
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
            ProfileName = "Cash Drawer Receipt Printer",
            SystemPrinterName = "POS-80",
            Role = PrinterRole.Receipt,
            SupportsCashDrawer = true,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        var receiptData = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-CARD-01",
            DailySalesNumber: 1,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new PrintableReceiptItem("Burger", "SKU-B", 1m, 200m, 200m)],
            Subtotal: 200m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 200m, Balance: 0m,
            Payments: [new PrintableReceiptPayment("Credit Card", 200m)]); // Non-cash payment

        // Act: Original customer copy, but paid with Credit Card
        var result = await service.DispatchReceiptAsync(
            receiptData,
            isReprint: false,
            isAdditionalCopy: false,
            copyType: ReceiptCopyType.Customer);

        // Assert: Cash drawer kick MUST be suppressed for non-cash tenders
        Assert.True(result.Success);
        Assert.NotNull(adapter.LastDispatchedProfile);
        Assert.False(adapter.LastDispatchedProfile.SupportsCashDrawer, "Cash drawer must NOT kick for non-cash payments.");
    }

    [Fact]
    public async Task CashDrawer_Triggered_OnPrimaryCustomerCopy_WithCashPayment()
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
            ProfileName = "Cash Drawer Receipt Printer",
            SystemPrinterName = "POS-80",
            Role = PrinterRole.Receipt,
            SupportsCashDrawer = true,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        var receiptData = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-CASH-01",
            DailySalesNumber: 1,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new PrintableReceiptItem("Burger", "SKU-B", 1m, 200m, 200m)],
            Subtotal: 200m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 200m, Balance: 0m,
            Payments: [new PrintableReceiptPayment("Cash", 200m)]); // Cash payment

        // Act: Original primary customer copy of cash tender
        var result = await service.DispatchReceiptAsync(
            receiptData,
            isReprint: false,
            isAdditionalCopy: false,
            copyType: ReceiptCopyType.Customer);

        // Assert: Cash drawer kick IS permitted on the primary customer copy of cash tenders
        Assert.True(result.Success);
        Assert.NotNull(adapter.LastDispatchedProfile);
        Assert.True(adapter.LastDispatchedProfile.SupportsCashDrawer, "Cash drawer MUST be allowed for primary customer copy with cash tender.");
    }

    [Fact]
    public async Task SaveTerminalReceiptSettingsAsync_BoundaryCopyCount_1Copy_Succeeds()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter);

        var terminalId = Guid.NewGuid();
        var settings = new TerminalReceiptSettings
        {
            TerminalId = terminalId,
            CustomerCopyEnabled = true,
            CustomerCopyCount = 1, // Min boundary
            MerchantCopyEnabled = true,
            MerchantCopyCount = 1
        };

        // Act
        await service.SaveTerminalReceiptSettingsAsync(terminalId, settings);
        var loaded = await service.GetTerminalReceiptSettingsAsync(terminalId);

        // Assert
        Assert.Equal(1, loaded.CustomerCopyCount);
        Assert.Equal(1, loaded.MerchantCopyCount);
    }

    [Fact]
    public async Task SaveTerminalReceiptSettingsAsync_BoundaryCopyCount_5Copies_Succeeds()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter);

        var terminalId = Guid.NewGuid();
        var settings = new TerminalReceiptSettings
        {
            TerminalId = terminalId,
            CustomerCopyEnabled = true,
            CustomerCopyCount = 5, // Max boundary (shared limit of 5)
            MerchantCopyEnabled = true,
            MerchantCopyCount = 5
        };

        // Act
        await service.SaveTerminalReceiptSettingsAsync(terminalId, settings);
        var loaded = await service.GetTerminalReceiptSettingsAsync(terminalId);

        // Assert
        Assert.Equal(5, loaded.CustomerCopyCount);
        Assert.Equal(5, loaded.MerchantCopyCount);
    }

    [Fact]
    public async Task SaveTerminalReceiptSettingsAsync_BoundaryCopyCount_6Copies_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var service = new PrinterManagementService(queueProvider, store, router, tracker, adapter);

        var terminalId = Guid.NewGuid();
        var settings = new TerminalReceiptSettings
        {
            TerminalId = terminalId,
            CustomerCopyEnabled = true,
            CustomerCopyCount = 6, // Exceeds MaxCopyCount of 5!
            MerchantCopyEnabled = false
        };

        // Act & Assert: Throws explicit ArgumentOutOfRangeException requiring correction (does not silently truncate)
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.SaveTerminalReceiptSettingsAsync(terminalId, settings));

        Assert.Contains("Configured customer copy count (6) exceeds supported range (1-5)", ex.Message);
        Assert.Contains("Please correct the configuration", ex.Message);
    }
}
