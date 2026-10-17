using System.Text.Json;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Printing;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Platform.Printing;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Desktop.Tests.Printing;

public class BackOfficeAndPosReceiptIntegrationTests : IDisposable
{
    private readonly string _testPosSettingsPath;

    public BackOfficeAndPosReceiptIntegrationTests()
    {
        _testPosSettingsPath = Path.Combine(Path.GetTempPath(), $"pos_settings_test_{Guid.NewGuid():N}.json");
        PosSettingsStore.SetTestingOverrides(_testPosSettingsPath);
    }

    public void Dispose()
    {
        PosSettingsStore.ResetTestingOverrides();
        try
        {
            if (File.Exists(_testPosSettingsPath)) File.Delete(_testPosSettingsPath);
        }
        catch { }
    }

    [Fact]
    public async Task TerminalReceiptSettings_IndependentCustomerAndMerchantCopies_PersistAcrossStore()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"printer_config_{Guid.NewGuid():N}.json");
        try
        {
            var store = new WindowsPrinterConfigurationStore(tempFile);
            var queueProvider = new WindowsPrinterQueueProvider();
            var tracker = new InMemoryPrinterJobTracker();
            var adapter = new WindowsSpoolerPrinterAdapter(queueProvider);
            var service = new PrinterManagementService(queueProvider, store, new PrinterRouter(store), tracker, adapter);

            // 1. Create two printer profiles (one for customer, one for merchant accounting)
            var customerProfile = new PrinterProfile
            {
                Id = Guid.NewGuid(),
                ProfileName = "Customer Front Counter",
                SystemPrinterName = "Microsoft Print to PDF",
                Role = PrinterRole.Receipt,
                PaperWidth = PaperWidth.Width80mm,
                CharactersPerLine = 42,
                SupportsCutter = true,
                SupportsCashDrawer = true
            };

            var merchantProfile = new PrinterProfile
            {
                Id = Guid.NewGuid(),
                ProfileName = "Back Office Accounting",
                SystemPrinterName = "OneNote (Desktop)",
                Role = PrinterRole.Receipt,
                PaperWidth = PaperWidth.Width80mm,
                CharactersPerLine = 42,
                SupportsCutter = false,
                SupportsCashDrawer = false
            };

            await service.SaveProfileAsync(customerProfile);
            await service.SaveProfileAsync(merchantProfile);

            // 2. Configure terminal-specific receipt settings
            var terminalId = Guid.NewGuid();
            var settings = new TerminalReceiptSettings
            {
                TerminalId = terminalId,
                CustomerCopyEnabled = true,
                CustomerCopyProfileId = customerProfile.Id,
                CustomerCopyCount = 1,
                MerchantCopyEnabled = true,
                MerchantCopyProfileId = merchantProfile.Id,
                MerchantCopyCount = 2,
                PaperWidth = PaperWidth.Width80mm,
                MarginMm = 4,
                CharactersPerLine = 42,
                HeaderText = "Clovent Flagship Store",
                FooterText = "GST Included. Visit Again!",
                EnableLogo = true,
                LogoPath = "C:\\Store\\logo.png"
            };

            await service.SaveTerminalReceiptSettingsAsync(terminalId, settings);
            await service.AssignTerminalDefaultReceiptPrinterAsync(terminalId, customerProfile.Id);

            // 3. Reload from store (simulating app restart) and verify persistence
            var freshStore = new WindowsPrinterConfigurationStore(tempFile);
            var reloadedConfig = await freshStore.LoadAsync();
            var reloadedSettings = reloadedConfig.GetSettingsForTerminal(terminalId);

            Assert.True(reloadedSettings.CustomerCopyEnabled);
            Assert.Equal(customerProfile.Id, reloadedSettings.CustomerCopyProfileId);
            Assert.Equal(1, reloadedSettings.CustomerCopyCount);

            Assert.True(reloadedSettings.MerchantCopyEnabled);
            Assert.Equal(merchantProfile.Id, reloadedSettings.MerchantCopyProfileId);
            Assert.Equal(2, reloadedSettings.MerchantCopyCount);

            Assert.Equal("Clovent Flagship Store", reloadedSettings.HeaderText);
            Assert.Equal("GST Included. Visit Again!", reloadedSettings.FooterText);
            Assert.Equal(4, reloadedSettings.MarginMm);
            Assert.True(reloadedSettings.EnableLogo);
            Assert.Equal("C:\\Store\\logo.png", reloadedSettings.LogoPath);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReceiptSnapshotFormatter_DraftBill_AlwaysRendersDraftNoticeAndNeverFinalBanner()
    {
        var printable = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-DRAFT-999",
            DailySalesNumber: null,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items:
            [
                new("Ribeye Steak", "STEAK-01", 1, 35.00m, 35.00m, null)
            ],
            Subtotal: 35.00m,
            TaxTotal: 2.80m,
            DiscountTotal: 0m,
            ServiceChargeTotal: 0m,
            GrandTotal: 37.80m,
            Balance: 37.80m,
            Payments: []);

        var options = new ReceiptRenderOptions(CopyType: ReceiptCopyType.Draft);
        var result = ReceiptSnapshotFormatter.FormatDraftBill(printable, options);

        Assert.Contains("*** DRAFT BILL / NOT A FINAL RECEIPT ***", result.FormattedText);
        Assert.DoesNotContain("*** CUSTOMER COPY ***", result.FormattedText);
        Assert.DoesNotContain("*** MERCHANT / STORE COPY ***", result.FormattedText);
        Assert.DoesNotContain("*** REPRINT", result.FormattedText);
    }

    [Fact]
    public void ReceiptSnapshotFormatter_Reprint_RendersSequentialCopyBannerAndPreservesHistoricalFigures()
    {
        var printable = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-HISTORIC-101",
            DailySalesNumber: 55,
            OrderType: "TakeAway",
            CompletedAtUtc: new DateTimeOffset(2026, 3, 15, 14, 30, 0, TimeSpan.Zero),
            Items:
            [
                new("BBQ Wings 12pc", "WINGS-12", 2, 12.50m, 25.00m, null),
                new("Garlic Bread", "BREAD-01", 1, 4.50m, 4.50m, null)
            ],
            Subtotal: 29.50m,
            TaxTotal: 2.36m,
            DiscountTotal: 3.00m,
            ServiceChargeTotal: 0m,
            GrandTotal: 28.86m,
            Balance: 0m,
            Payments:
            [
                new("MasterCard", 28.86m)
            ],
            CashierName: "Sarah Cashier",
            CustomerNotes: "Pack separately");

        var options = new ReceiptRenderOptions(
            IsReprint: true,
            ReprintCount: 3,
            ReprintReason: "Customer requested duplicate for company expense claim",
            CopyType: ReceiptCopyType.Reprint);

        var result = ReceiptSnapshotFormatter.Format(printable, options);

        Assert.Contains("*** REPRINT (COPY #3) ***", result.FormattedText);
        Assert.Contains("Reason: Customer requested duplicate for company expense claim", result.FormattedText);
        Assert.Contains("ORD-HISTORIC-101", result.FormattedText);
        Assert.Contains("28.86", result.FormattedText);
        Assert.Contains("Sarah Cashier", result.FormattedText);
    }

    [Fact]
    public void BlackCopper85Detector_DetectsDriverCapabilitiesAndLeavesCapabilitiesUnverifiedUntilPhysicallyTested()
    {
        var bc85 = BlackCopper85Detector.Inspect("Black Copper 85 High-Speed Thermal");
        Assert.True(bc85.IsBlackCopper85);
        Assert.True(bc85.IsThermalReceipt);
        // Queue/driver name alone must NOT establish cutter or drawer support
        Assert.False(bc85.SupportsCutter);
        Assert.False(bc85.SupportsCashDrawer);
        Assert.Equal("Unverified", bc85.HardwareVerificationStatus);
        Assert.Contains("Unverified", bc85.ModelDescription);
        Assert.Equal(PaperWidth.Width80mm, bc85.RecommendedPaperWidth);
        Assert.Equal(42, bc85.RecommendedColumns);

        var virtualPdf = BlackCopper85Detector.Inspect("Microsoft Print to PDF");
        Assert.False(virtualPdf.IsBlackCopper85);
        Assert.False(virtualPdf.SupportsCutter);
        Assert.False(virtualPdf.SupportsCashDrawer);
        Assert.Equal("Unsupported (Virtual Device)", virtualPdf.HardwareVerificationStatus);
    }

    [Fact]
    public async Task PrinterManagementService_CashDrawerSuppression_EnforcesCashTendersAndPrimaryCustomerCopy()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"drawer_test_{Guid.NewGuid():N}.json");
        try
        {
            var store = new WindowsPrinterConfigurationStore(tempFile);
            var queueProvider = new WindowsPrinterQueueProvider();
            var tracker = new InMemoryPrinterJobTracker();
            var capturedProfiles = new List<PrinterProfile>();

            var adapter = new TestCapturingPrinterAdapter(profile => capturedProfiles.Add(profile));
            var service = new PrinterManagementService(queueProvider, store, new PrinterRouter(store), tracker, adapter);

            var profile = new PrinterProfile
            {
                Id = Guid.NewGuid(),
                ProfileName = "Thermal Test Printer",
                SystemPrinterName = "Microsoft Print to PDF", // using installed Windows queue
                Role = PrinterRole.Receipt,
                PaperWidth = PaperWidth.Width80mm,
                CharactersPerLine = 42,
                SupportsCutter = true,
                SupportsCashDrawer = true
            };
            await service.SaveProfileAsync(profile);

            var cashPayment = new PrintableReceiptData(
                OrderId: Guid.NewGuid(),
                OrderNumber: "ORD-DRAWER-01",
                DailySalesNumber: 1,
                OrderType: "DineIn",
                CompletedAtUtc: DateTimeOffset.UtcNow,
                Items: [new("Burger", "B1", 1, 10m, 10m)],
                Subtotal: 10m,
                TaxTotal: 0m,
                DiscountTotal: 0m,
                ServiceChargeTotal: 0m,
                GrandTotal: 10m,
                Balance: 0m,
                Payments: [new("Cash", 10m)]);

            var cardPayment = new PrintableReceiptData(
                OrderId: Guid.NewGuid(),
                OrderNumber: "ORD-DRAWER-02",
                DailySalesNumber: 2,
                OrderType: "DineIn",
                CompletedAtUtc: DateTimeOffset.UtcNow,
                Items: [new("Burger", "B1", 1, 10m, 10m)],
                Subtotal: 10m,
                TaxTotal: 0m,
                DiscountTotal: 0m,
                ServiceChargeTotal: 0m,
                GrandTotal: 10m,
                Balance: 0m,
                Payments: [new("MasterCard", 10m)]);

            // 1. Primary customer copy with Cash -> Cash drawer kick ALLOWED
            capturedProfiles.Clear();
            await service.DispatchReceiptAsync(cashPayment, targetProfileId: profile.Id, copyType: ReceiptCopyType.Customer, isAdditionalCopy: false);
            Assert.Single(capturedProfiles);
            Assert.True(capturedProfiles[0].SupportsCashDrawer);

            // 2. Additional customer copy (copy #2) with Cash -> Cash drawer kick SUPPRESSED
            capturedProfiles.Clear();
            await service.DispatchReceiptAsync(cashPayment, targetProfileId: profile.Id, copyType: ReceiptCopyType.Customer, isAdditionalCopy: true);
            Assert.Single(capturedProfiles);
            Assert.False(capturedProfiles[0].SupportsCashDrawer);

            // 3. Merchant copy with Cash -> Cash drawer kick SUPPRESSED
            capturedProfiles.Clear();
            await service.DispatchReceiptAsync(cashPayment, targetProfileId: profile.Id, copyType: ReceiptCopyType.Merchant);
            Assert.Single(capturedProfiles);
            Assert.False(capturedProfiles[0].SupportsCashDrawer);

            // 4. Reprint with Cash -> Cash drawer kick SUPPRESSED
            capturedProfiles.Clear();
            await service.DispatchReceiptAsync(cashPayment, targetProfileId: profile.Id, isReprint: true, copyType: ReceiptCopyType.Reprint);
            Assert.Single(capturedProfiles);
            Assert.False(capturedProfiles[0].SupportsCashDrawer);

            // 5. Card payment primary customer copy -> Cash drawer kick SUPPRESSED (non-cash)
            capturedProfiles.Clear();
            await service.DispatchReceiptAsync(cardPayment, targetProfileId: profile.Id, copyType: ReceiptCopyType.Customer, isAdditionalCopy: false);
            Assert.Single(capturedProfiles);
            Assert.False(capturedProfiles[0].SupportsCashDrawer);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void DurableFilePrinterJobTracker_AppRestartRecovery_MarksInterruptedJobsAmbiguous()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"tracker_restart_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);

            var profileId = Guid.NewGuid();
            var job1 = PrinterJob.Create("CustomerReceipt", profileId, "Queue1", "Sample Payload 1");
            var job2 = PrinterJob.Create("CustomerReceipt", profileId, "Queue1", "Sample Payload 2");

            // Session 1: Submit job1 and leave job2 in-flight before simulated abrupt termination
            var tracker1 = new DurableFilePrinterJobTracker(tempDir);
            tracker1.RecordJob(job1);
            tracker1.MarkSubmitted(job1.JobId);

            tracker1.RecordJob(job2);
            tracker1.MarkInFlight(job2.JobId);

            // Session 2: Fresh instance simulating application restart
            var tracker2 = new DurableFilePrinterJobTracker(tempDir);

            var recoveredJob1 = tracker2.GetJob(job1.JobId);
            var recoveredJob2 = tracker2.GetJob(job2.JobId);

            Assert.NotNull(recoveredJob1);
            Assert.NotNull(recoveredJob2);

            // Invariant: Interrupted submissions MUST be recovered into Ambiguous status (never claiming completed printing)
            Assert.Equal(PrinterJobStatus.Ambiguous, recoveredJob1.Status);
            Assert.Contains("Interrupted across restart", recoveredJob1.ErrorMessage);

            Assert.Equal(PrinterJobStatus.Ambiguous, recoveredJob2.Status);
            Assert.Contains("Interrupted across restart", recoveredJob2.ErrorMessage);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void PosSettingsStore_EnsureWorkstationScopeOwnership_EnforcesScopeMismatchRejection()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"pos_scope_{Guid.NewGuid():N}.json");
        try
        {
            PosSettingsStore.SetTestingOverrides(tempFile);

            var activeTerminalId = Guid.NewGuid();
            var activeBranchId = Guid.NewGuid();
            var activeOrgId = Guid.NewGuid();

            PosSettingsStore.SaveTerminalId(activeTerminalId);
            PosSettingsStore.SaveBranchId(activeBranchId);
            PosSettingsStore.SaveOrganizationId(activeOrgId);

            // 1. Same scope passes
            PosSettingsStore.EnsureWorkstationScopeOwnership(activeOrgId, activeBranchId, activeTerminalId, "valid test action");

            // 2. Mismatched organization fails
            var foreignOrgId = Guid.NewGuid();
            var orgEx = Assert.Throws<UnauthorizedAccessException>(() =>
                PosSettingsStore.EnsureWorkstationScopeOwnership(foreignOrgId, activeBranchId, activeTerminalId, "foreign org action"));
            Assert.Contains("organization", orgEx.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Manager/Admin status cannot bypass ownership boundaries", orgEx.Message);

            // 3. Mismatched branch fails
            var foreignBranchId = Guid.NewGuid();
            var branchEx = Assert.Throws<UnauthorizedAccessException>(() =>
                PosSettingsStore.EnsureWorkstationScopeOwnership(activeOrgId, foreignBranchId, activeTerminalId, "foreign branch action"));
            Assert.Contains("branch", branchEx.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Manager/Admin status cannot bypass ownership boundaries", branchEx.Message);

            // 4. Mismatched terminal fails
            var foreignTerminalId = Guid.NewGuid();
            var termEx = Assert.Throws<UnauthorizedAccessException>(() =>
                PosSettingsStore.EnsureWorkstationScopeOwnership(activeOrgId, activeBranchId, foreignTerminalId, "foreign terminal action"));
            Assert.Contains("terminal", termEx.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Manager/Admin status cannot bypass ownership boundaries", termEx.Message);
        }
        finally
        {
            PosSettingsStore.ResetTestingOverrides();
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void EnhancedReceiptPrintDocument_AcceptsPaperWidthMarginsAndPhysicallyRendersLogo()
    {
        var tempLogo = Path.Combine(Path.GetTempPath(), $"logo_{Guid.NewGuid():N}.bmp");
        try
        {
            // Create a small 50x50 bitmap for testing
            using (var bmp = new System.Drawing.Bitmap(50, 50))
            {
                using var g = System.Drawing.Graphics.FromImage(bmp);
                g.Clear(System.Drawing.Color.Black);
                bmp.Save(tempLogo, System.Drawing.Imaging.ImageFormat.Bmp);
            }

            var receiptText = "[DEMO LOGO]\nStore Header\nItem 1    $10.00\nTotal     $10.00";
            using var doc = new EnhancedReceiptPrintDocument(
                receiptText,
                paperWidth: PaperWidth.Width80mm,
                containsComplexScript: false,
                marginMm: 5,
                logoPath: tempLogo);

            // Margin check: 5mm * 3.937 = ~20 hundredths of an inch
            Assert.Equal(20, doc.DefaultPageSettings.Margins.Left);
            Assert.Equal(20, doc.DefaultPageSettings.Margins.Right);

            // Width check: 80mm roll = 315 hundredths of an inch
            Assert.Equal(315, doc.DefaultPageSettings.PaperSize.Width);
        }
        finally
        {
            if (File.Exists(tempLogo)) File.Delete(tempLogo);
        }
    }

    private sealed class TestCapturingPrinterAdapter : IPrinterAdapter
    {
        private readonly Action<PrinterProfile> _onDispatch;

        public TestCapturingPrinterAdapter(Action<PrinterProfile> onDispatch)
        {
            _onDispatch = onDispatch;
        }

        public PrinterConnectionType SupportedConnection => PrinterConnectionType.WindowsDriver;

        public Task<bool> IsAvailableAsync(PrinterProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<PrinterHealthSnapshot> CheckHealthAsync(PrinterProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult(PrinterHealthSnapshot.Healthy(profile.SystemPrinterName, profile.Id));

        public Task<PrintDispatchResult> DispatchAsync(PrinterProfile profile, PrinterJob job, CancellationToken cancellationToken = default)
        {
            _onDispatch(profile);
            return Task.FromResult(PrintDispatchResult.SpoolerAccepted(profile.SystemPrinterName));
        }
    }
}
