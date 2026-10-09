using System.Text.Json;
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
        try
        {
            if (File.Exists(_tempConfigFile)) File.Delete(_tempConfigFile);
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
    public async Task PrintReceiptAsync_WithCorruptSnapshotJson_FallsBackToReceiptText()
    {
        // Arrange
        var payload = new ReceiptPrintPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-CORRUPT-002",
            ReceiptText: "=== FALLBACK TEXT RECEIPT ===",
            SnapshotJson: "{ malformed json content !!! }");

        // Act
        var result = await _receiptPrintService.PrintReceiptAsync(payload);

        // Assert
        Assert.Equal(ReceiptPrintStatus.Printed, result.Status);
        Assert.Equal("POS-Receipt", result.PrinterName);
        Assert.Equal(1, _adapter.DispatchCount);
        Assert.NotNull(_adapter.LastDispatchedJob);
        Assert.Equal("=== FALLBACK TEXT RECEIPT ===", _adapter.LastDispatchedJob.PayloadText);
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
}
