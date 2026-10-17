using System.Text.Json;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Printing;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Platform.Printing;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Printing;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Desktop.Tests.Printing;

public class DesktopReceiptPrintServiceTests : IDisposable
{
    private readonly string _tempConfigFile;
    private readonly string _tempPosSettingsFile;
    private readonly FakeQueueProvider _queueProvider = new();
    private readonly FakePrinterAdapter _adapter = new();
    private readonly FakeCurrentSession _session = new();
    private readonly FakeAuthorizationService _auth = new();
    private readonly WindowsPrinterConfigurationStore _configStore;
    private readonly PrinterRouter _router;
    private readonly InMemoryPrinterJobTracker _tracker = new();
    private readonly PrinterManagementService _managementService;
    private readonly DesktopReceiptPrintService _receiptPrintService;

    private sealed class FakeQueueProvider : IWindowsPrinterQueueProvider
    {
        public List<WindowsPrinterInfo> Installed { get; } = [new("POS-Receipt", true, false, "Ready")];
        public IReadOnlyList<WindowsPrinterInfo> GetInstalledPrinters() => Installed;
        public bool IsPrinterInstalled(string printerName) => Installed.Any(p => p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));
        public string? GetDefaultPrinterName() => "POS-Receipt";
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
                ? PrinterHealthSnapshot.Faulted(profile.SystemPrinterName ?? "TestPrinter", PrinterHardwareCondition.Offline, "Device unavailable", profile.Id)
                : PrinterHealthSnapshot.Healthy(profile.SystemPrinterName ?? "TestPrinter", profile.Id));

        public Task<PrintDispatchResult> DispatchAsync(PrinterProfile profile, PrinterJob job, CancellationToken cancellationToken = default)
        {
            DispatchCount++;
            LastDispatchedJob = job;

            if (ShouldFail)
            {
                return Task.FromResult(PrintDispatchResult.Failed(profile.SystemPrinterName, "Device unavailable"));
            }

            return Task.FromResult(PrintDispatchResult.SpoolerAccepted(profile.SystemPrinterName));
        }
    }

    private sealed class FakeCurrentSession : ICurrentSession
    {
        public bool IsAuthenticated { get; set; } = true;
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Cashier Operator";
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
    }

    private sealed class FakeAuthorizationService : IAuthorizationService
    {
        public HashSet<string> GrantedPermissions { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "feature.printers.manage",
            "feature.printers.test",
            "feature.printers.view"
        };

        public Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyCollection<string>)GrantedPermissions.ToList());

        public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(GrantedPermissions.Contains(permissionCode));

        public Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> SatisfiesPolicyAsync(Guid userId, string policyName, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    public DesktopReceiptPrintServiceTests()
    {
        _tempConfigFile = Path.Combine(Path.GetTempPath(), $"receipt-test-{Guid.NewGuid():N}.json");
        _tempPosSettingsFile = Path.Combine(Path.GetTempPath(), $"pos-settings-test-{Guid.NewGuid():N}.json");
        PosSettingsStore.SetTestingOverrides(_tempPosSettingsFile);

        _configStore = new WindowsPrinterConfigurationStore(_tempConfigFile);

        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Front POS 80mm",
            SystemPrinterName = "POS-Receipt",
            ConnectionType = PrinterConnectionType.WindowsDriver,
            PaperWidth = PaperWidth.Width80mm,
            Role = PrinterRole.Receipt,
            IsEnabled = true
        };

        var config = new PrinterConfiguration
        {
            Profiles = [profile],
            DefaultReceiptProfileId = profile.Id
        };
        _configStore.SaveAsync(config).GetAwaiter().GetResult();

        _router = new PrinterRouter(_configStore);

        _managementService = new PrinterManagementService(
            _queueProvider,
            _configStore,
            _router,
            _tracker,
            _adapter,
            _auth,
            _session);

        _receiptPrintService = new DesktopReceiptPrintService(_managementService);
    }

    public void Dispose()
    {
        PosSettingsStore.ResetTestingOverrides();
        try
        {
            if (File.Exists(_tempConfigFile)) File.Delete(_tempConfigFile);
            if (File.Exists(_tempPosSettingsFile)) File.Delete(_tempPosSettingsFile);
        }
        catch { }
    }

    [Fact]
    public async Task PrintReceiptAsync_WithValidSnapshotJson_DispatchesStructuredReceiptSuccessfully()
    {
        // Arrange
        var snapshot = new ReceiptSnapshot(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-2026-001",
            DailySalesNumber: 1,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items:
            [
                new ReceiptSnapshotItem(
                    ProductVariantId: Guid.NewGuid(),
                    Sku: "SKU-001",
                    Name: "Biryani Special",
                    Quantity: 2m,
                    UnitPrice: 500m,
                    LineTotal: 1000m,
                    Notes: null)
            ],
            Subtotal: 1000m,
            TaxTotal: 160m,
            DiscountTotal: 0m,
            ServiceChargeTotal: 0m,
            GrandTotal: 1160m,
            Balance: 0m,
            Payments:
            [
                new ReceiptSnapshotPayment(1160m, "Cash")
            ],
            CashierName: "Cashier 1",
            TerminalName: "TERM-01",
            CustomerNotes: null);

        var snapshotJson = JsonSerializer.Serialize(snapshot);
        var payload = new ReceiptPrintPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-2026-001",
            ReceiptText: "Plain fallback text",
            SnapshotJson: snapshotJson);

        // Act
        var result = await _receiptPrintService.PrintReceiptAsync(payload);

        // Assert
        Assert.Equal(ReceiptPrintStatus.Printed, result.Status);
        Assert.Equal("POS-Receipt", result.PrinterName);
        Assert.Equal(1, _adapter.DispatchCount);
        Assert.NotNull(_adapter.LastDispatchedJob);
        Assert.Contains("ORD-2026-001", _adapter.LastDispatchedJob.PayloadText);
    }

    [Fact]
    public async Task PrintReceiptAsync_WithCorruptSnapshotJson_RejectsFallbackAndReturnsPrintFailed()
    {
        // Arrange
        var payload = new ReceiptPrintPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-CORRUPT-002",
            ReceiptText: "=== FALLBACK TEXT RECEIPT ===",
            SnapshotJson: "{ malformed json content !!! }");

        // Act
        var result = await _receiptPrintService.PrintReceiptAsync(payload);

        // Assert: Corruption must not silently become successful fallback printing
        Assert.Equal(ReceiptPrintStatus.PrintFailed, result.Status);
        Assert.Contains("corrupted", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, _adapter.DispatchCount);
    }

    [Fact]
    public async Task PrintReceiptAsync_WithNullSnapshotJson_DispatchesReceiptTextDirectly()
    {
        // Arrange
        var payload = new ReceiptPrintPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-LEGACY-003",
            ReceiptText: "=== LEGACY PAYLOAD TEXT ===",
            SnapshotJson: null);

        // Act
        var result = await _receiptPrintService.PrintReceiptAsync(payload);

        // Assert
        Assert.Equal(ReceiptPrintStatus.Printed, result.Status);
        Assert.Equal("POS-Receipt", result.PrinterName);
        Assert.Equal(1, _adapter.DispatchCount);
        Assert.NotNull(_adapter.LastDispatchedJob);
        Assert.Equal("=== LEGACY PAYLOAD TEXT ===", _adapter.LastDispatchedJob.PayloadText);
    }

    [Fact]
    public async Task PrintReceiptAsync_WhenSimulatingOutage_ThrowsInvalidOperationException()
    {
        // Arrange
        _receiptPrintService.SetSimulatedOutage(true);
        var payload = new ReceiptPrintPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-OUTAGE-004",
            ReceiptText: "Outage text",
            SnapshotJson: null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _receiptPrintService.PrintReceiptAsync(payload));
        Assert.Equal(0, _adapter.DispatchCount);
    }

    [Fact]
    public async Task PrintReceiptAsync_WhenPrinterAdapterFails_ReturnsPrintFailedStatus()
    {
        // Arrange
        _adapter.ShouldFail = true;
        var payload = new ReceiptPrintPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-FAIL-005",
            ReceiptText: "Failure test text",
            SnapshotJson: null);

        // Act
        var result = await _receiptPrintService.PrintReceiptAsync(payload);

        // Assert
        Assert.Equal(ReceiptPrintStatus.PrintFailed, result.Status);
        Assert.Equal("POS-Receipt", result.PrinterName);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public async Task PrintReceiptAsync_PassesStableOutboxMessageIdAsJobIdAndCapturedContext()
    {
        // Arrange
        var outboxMessageId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();

        var payload = new ReceiptPrintPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-CONTEXT-006",
            ReceiptText: "=== CONTEXT TEST ===",
            SnapshotJson: null,
            OutboxMessageId: outboxMessageId,
            OrganizationId: orgId,
            BranchId: branchId,
            TerminalId: terminalId);

        // Act
        var result = await _receiptPrintService.PrintReceiptAsync(payload);

        // Assert
        Assert.Equal(ReceiptPrintStatus.Printed, result.Status);
        Assert.Equal(1, _adapter.DispatchCount);
        Assert.NotNull(_adapter.LastDispatchedJob);
        Assert.Equal(outboxMessageId, _adapter.LastDispatchedJob.JobId);
        Assert.Equal(orgId, _adapter.LastDispatchedJob.OrganizationId);
        Assert.Equal(branchId, _adapter.LastDispatchedJob.BranchId);
        Assert.Equal(terminalId, _adapter.LastDispatchedJob.TerminalId);
    }

    [Fact]
    public async Task PrintReceiptAsync_WithStructurallyInvalidSnapshot_RejectsFallbackAndReturnsPrintFailed()
    {
        // Arrange: Valid JSON syntax, but structurally invalid (empty order number and no items)
        var invalidSnapshot = new ReceiptSnapshot(
            OrderId: Guid.NewGuid(),
            OrderNumber: "", // empty!
            DailySalesNumber: null,
            OrderType: "TakeAway",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [], // no items!
            Subtotal: 0m,
            TaxTotal: 0m,
            DiscountTotal: 0m,
            ServiceChargeTotal: 0m,
            GrandTotal: 0m,
            Balance: 0m,
            Payments: [],
            CashierName: null,
            TerminalName: null,
            CustomerNotes: null);

        var payload = new ReceiptPrintPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-INVALID-007",
            ReceiptText: "=== FALLBACK TEXT THAT MUST BE REJECTED ===",
            SnapshotJson: JsonSerializer.Serialize(invalidSnapshot));

        // Act
        var result = await _receiptPrintService.PrintReceiptAsync(payload);

        // Assert: Structurally invalid snapshot must not fall back to text printing
        Assert.Equal(ReceiptPrintStatus.PrintFailed, result.Status);
        Assert.Contains("structurally invalid", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, _adapter.DispatchCount);
    }

    [Fact]
    public async Task PrintReceiptAsync_WithEmptyLegacyText_ReturnsPrintFailed()
    {
        // Arrange: Legacy payload with null snapshot and whitespace-only receipt text
        var payload = new ReceiptPrintPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-EMPTY-008",
            ReceiptText: "   ",
            SnapshotJson: null);

        // Act
        var result = await _receiptPrintService.PrintReceiptAsync(payload);

        // Assert
        Assert.Equal(ReceiptPrintStatus.PrintFailed, result.Status);
        Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, _adapter.DispatchCount);
    }

    [Fact]
    public async Task PrintReceiptAsync_SnapshotScopePrecedenceOverCallerPayload_UsesSnapshotOwnership()
    {
        // Arrange: Snapshot has frozen sale-time scope
        var snapshotOrg = Guid.NewGuid();
        var snapshotBranch = Guid.NewGuid();
        var snapshotTerminal = Guid.NewGuid();

        var callerOrg = Guid.NewGuid();
        var callerBranch = Guid.NewGuid();
        var callerTerminal = Guid.NewGuid();

        var snapshot = new ReceiptSnapshot(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-SCOPE-009",
            DailySalesNumber: 5,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items: [new ReceiptSnapshotItem(Guid.NewGuid(), "SKU-1", "Item", 1m, 100m, 100m, null)],
            Subtotal: 100m, TaxTotal: 0m, DiscountTotal: 0m, ServiceChargeTotal: 0m, GrandTotal: 100m, Balance: 0m,
            Payments: [new ReceiptSnapshotPayment(100m, "Cash")],
            CashierName: "Cashier",
            TerminalName: "TERM-1",
            CustomerNotes: null,
            OrganizationId: snapshotOrg,
            BranchId: snapshotBranch,
            TerminalId: snapshotTerminal);

        var payload = new ReceiptPrintPayload(
            OrderId: snapshot.OrderId,
            OrderNumber: snapshot.OrderNumber,
            ReceiptText: "Fallback text",
            SnapshotJson: JsonSerializer.Serialize(snapshot),
            OrganizationId: callerOrg,
            BranchId: callerBranch,
            TerminalId: callerTerminal);

        // Act
        var result = await _receiptPrintService.PrintReceiptAsync(payload);

        // Assert: Immutable snapshot ownership takes precedence over caller-supplied scopes
        Assert.Equal(ReceiptPrintStatus.Printed, result.Status);
        Assert.Equal(1, _adapter.DispatchCount);
        Assert.NotNull(_adapter.LastDispatchedJob);
        Assert.Equal(snapshotOrg, _adapter.LastDispatchedJob.OrganizationId);
        Assert.Equal(snapshotBranch, _adapter.LastDispatchedJob.BranchId);
        Assert.Equal(snapshotTerminal, _adapter.LastDispatchedJob.TerminalId);
    }
}
