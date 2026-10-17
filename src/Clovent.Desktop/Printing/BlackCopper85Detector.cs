using Clovent.Platform.Printing;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Detailed hardware capability profile derived from Windows printer and driver inspection.
/// </summary>
public sealed record PrinterCapabilityProfile(
    string PrinterName,
    string DriverName,
    bool IsBlackCopper85,
    bool IsThermalReceipt,
    bool SupportsCutter,
    bool SupportsCashDrawer,
    PaperWidth RecommendedPaperWidth,
    int RecommendedColumns,
    string ModelDescription,
    string HardwareVerificationStatus = "Unverified");

/// <summary>
/// Hardware capability inspector for thermal receipt printers, with specialized
/// support and driver detection for the Black Copper 85 (BC-85 / BC-85AC) thermal receipt printer.
/// Ensures that queue and driver names alone do not claim verified cutter or drawer support.
/// Capabilities remain "Unverified" until physically confirmed on the actual device.
/// </summary>
public static class BlackCopper85Detector
{
    private static readonly string[] BlackCopperKeywords =
    [
        "black copper", "bc-85", "bc85", "bc-85ac", "bc 85", "blackcopper"
    ];

    private static readonly string[] ThermalKeywords =
    [
        "pos-80", "pos80", "pos-58", "pos58", "thermal", "receipt", "tm-t88", "tm-t20", "tsp100", "srp-350"
    ];

    private static readonly string[] VirtualOrDocumentKeywords =
    [
        "pdf", "onenote", "xps", "fax", "document writer", "virtual", "anydesk"
    ];

    /// <summary>
    /// Inspects the given printer queue and driver names, returning its capability profile.
    /// Driver and queue names alone do NOT establish cutter or cash drawer support.
    /// </summary>
    public static PrinterCapabilityProfile Inspect(string printerName, string? driverName = null)
    {
        var pName = printerName ?? string.Empty;
        var dName = driverName ?? string.Empty;
        var combined = $"{pName} {dName}".ToLowerInvariant();

        var isBlackCopper = BlackCopperKeywords.Any(k => combined.Contains(k));
        var isVirtual = VirtualOrDocumentKeywords.Any(k => combined.Contains(k));
        var isThermal = isBlackCopper || (!isVirtual && ThermalKeywords.Any(k => combined.Contains(k)));

        if (isBlackCopper)
        {
            return new PrinterCapabilityProfile(
                pName,
                dName,
                IsBlackCopper85: true,
                IsThermalReceipt: true,
                SupportsCutter: false,
                SupportsCashDrawer: false,
                RecommendedPaperWidth: PaperWidth.Width80mm,
                RecommendedColumns: 42,
                ModelDescription: "Black Copper 85 Driver Detected (Hardware capabilities Unverified - cutter and cash drawer require physical verification)",
                HardwareVerificationStatus: "Unverified");
        }

        if (isVirtual)
        {
            return new PrinterCapabilityProfile(
                pName,
                dName,
                IsBlackCopper85: false,
                IsThermalReceipt: false,
                SupportsCutter: false,
                SupportsCashDrawer: false,
                RecommendedPaperWidth: PaperWidth.A4Custom,
                RecommendedColumns: 80,
                ModelDescription: "Virtual / System Document Printer (Hardware cutter and cash-drawer commands disabled)",
                HardwareVerificationStatus: "Unsupported (Virtual Device)");
        }

        if (isThermal)
        {
            var is58mm = combined.Contains("58");
            return new PrinterCapabilityProfile(
                pName,
                dName,
                IsBlackCopper85: false,
                IsThermalReceipt: true,
                SupportsCutter: false,
                SupportsCashDrawer: false,
                RecommendedPaperWidth: is58mm ? PaperWidth.Width58mm : PaperWidth.Width80mm,
                RecommendedColumns: is58mm ? 32 : 42,
                ModelDescription: is58mm ? "58mm Thermal Receipt Printer (Hardware capabilities Unverified)" : "Standard 80mm Thermal Receipt Printer (Hardware capabilities Unverified)",
                HardwareVerificationStatus: "Unverified");
        }

        return new PrinterCapabilityProfile(
            pName,
            dName,
            IsBlackCopper85: false,
            IsThermalReceipt: false,
            SupportsCutter: false,
            SupportsCashDrawer: false,
            RecommendedPaperWidth: PaperWidth.Width80mm,
            RecommendedColumns: 42,
            ModelDescription: "Standard Windows Printer Queue (Hardware capabilities Unverified)",
            HardwareVerificationStatus: "Unverified");
    }
}
