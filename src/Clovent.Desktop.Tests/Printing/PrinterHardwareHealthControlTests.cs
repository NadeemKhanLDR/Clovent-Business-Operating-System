using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Notifications;
using Clovent.Desktop.Printing;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Printing;
using Xunit;

namespace Clovent.Desktop.Tests.Printing;

public sealed class PrinterHardwareHealthControlTests : IDisposable
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

    private sealed class FakePrinterAdapter : IPrinterAdapter
    {
        public PrinterConnectionType SupportedConnection => PrinterConnectionType.WindowsDriver;
        public Task<bool> IsAvailableAsync(PrinterProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<PrinterHealthSnapshot> CheckHealthAsync(PrinterProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(PrinterHealthSnapshot.Healthy(profile.SystemPrinterName, profile.Id));

        public Task<PrintDispatchResult> DispatchAsync(PrinterProfile profile, PrinterJob job, CancellationToken cancellationToken = default) =>
            Task.FromResult(PrintDispatchResult.SpoolerAccepted(profile.SystemPrinterName));
    }

    public PrinterHardwareHealthControlTests()
    {
        _tempConfigFile = Path.Combine(Path.GetTempPath(), $"cbos-ui-cfg-{Guid.NewGuid():N}.json");
        _tempQuarantineDir = Path.Combine(Path.GetTempPath(), $"cbos-ui-quarantine-{Guid.NewGuid():N}");
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

    [Fact]
    public void ParameterlessConstructor_VisualStudioDesignerSafe_DoesNotThrow()
    {
        // Visual Studio WinForms Designer instantiation safety
        using var control = new PrinterHardwareHealthControl();
        Assert.NotNull(control);
        Assert.True(control.Controls.Count > 0);
    }

    [Fact]
    public void PrinterHardwareHealthView_InitializesContainer_WithDockedControl()
    {
        using var view = new PrinterHardwareHealthView();
        Assert.NotNull(view);
        Assert.NotNull(view.HealthControl);
    }

    [Fact]
    public async Task RuntimeConstructor_WithDependencies_RefreshesDataWithoutError()
    {
        // Arrange
        var queueProvider = new FakeQueueProvider();
        var store = new WindowsPrinterConfigurationStore(_tempConfigFile);
        var router = new PrinterRouter(store);
        var tracker = new InMemoryPrinterJobTracker();
        var adapter = new FakePrinterAdapter();
        var quarantineStore = new PrintJobQuarantineStore(_tempQuarantineDir);
        var cbRegistry = new CircuitBreakerRegistry();

        var service = new PrinterManagementService(
            queueProvider, store, router, tracker, adapter,
            quarantineStore, cbRegistry);

        // Pre-configure a profile
        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = "Receipt Printer",
            SystemPrinterName = "POS-Thermal-80",
            Role = PrinterRole.Receipt,
            IsEnabled = true
        };
        await service.SaveProfileAsync(profile);

        // Quarantine a sample job into store
        var quarantinedJob = new QuarantinedPrintJob
        {
            JobId = Guid.NewGuid(),
            DocumentType = "CustomerReceipt",
            TargetPrinterProfileId = profile.Id,
            TargetSystemPrinterName = "POS-Thermal-80",
            PayloadText = "Sample Test Receipt",
            FailureReason = "Device temporarily offline",
            QuarantinedAtUtc = DateTimeOffset.UtcNow
        };
        await quarantineStore.QuarantineJobAsync(quarantinedJob);

        // Act
        using var control = new PrinterHardwareHealthControl(service, quarantineStore, cbRegistry);
        Assert.NotNull(control);

        // Trigger refresh
        await control.RefreshAllDataAsync();

        // Control layout and child hierarchies must be properly attached
        Assert.True(control.Controls.Count >= 3); // Summary panel, toolbar, tab control
    }
}
