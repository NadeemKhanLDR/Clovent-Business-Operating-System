namespace Clovent.Platform.Printing;

/// <summary>
/// A logical printer configuration profile representing a physical or virtual printer.
/// Identified by a stable UUID rather than a transient UI list index.
/// </summary>
public sealed class PrinterProfile
{
    /// <summary>Unique and stable identifier for this profile.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Human-readable display name (e.g. "Front Counter Thermal", "Kitchen Hot Prep").</summary>
    public string ProfileName { get; set; } = string.Empty;

    /// <summary>Underlying system queue name in Windows or target endpoint IP/port.</summary>
    public string SystemPrinterName { get; set; } = string.Empty;

    /// <summary>Default document role handled by this printer.</summary>
    public PrinterRole Role { get; set; } = PrinterRole.Receipt;

    /// <summary>Connection type and communication mechanism.</summary>
    public PrinterConnectionType ConnectionType { get; set; } = PrinterConnectionType.WindowsDriver;

    /// <summary>Physical paper width profile.</summary>
    public PaperWidth PaperWidth { get; set; } = PaperWidth.Width80mm;

    /// <summary>Monospace characters per line for formatted plain-text receipts.</summary>
    public int CharactersPerLine { get; set; } = 42;

    /// <summary>Left and right margin in millimeters.</summary>
    public int MarginMm { get; set; } = 2;

    /// <summary>Number of physical copies to produce per print request.</summary>
    public int PrintCopies { get; set; } = 1;

    /// <summary>Whether this device supports an automatic paper cutter command.</summary>
    public bool SupportsCutter { get; set; } = true;

    /// <summary>Whether this device is attached to a cash drawer kick connector (RJ11/RJ12).</summary>
    public bool SupportsCashDrawer { get; set; } = true;

    /// <summary>Whether this printer profile is active and available for routing.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Timestamp when this profile was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Timestamp when this profile was last modified.</summary>
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    /// <summary>Creates a default profile for 80 mm Windows spooler thermal printing.</summary>
    public static PrinterProfile CreateDefault(string systemPrinterName, string profileName = "Default Receipt Printer")
    {
        return new PrinterProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = profileName,
            SystemPrinterName = systemPrinterName,
            Role = PrinterRole.Receipt,
            ConnectionType = PrinterConnectionType.WindowsDriver,
            PaperWidth = PaperWidth.Width80mm,
            CharactersPerLine = 42,
            MarginMm = 2,
            PrintCopies = 1,
            SupportsCutter = true,
            SupportsCashDrawer = true,
            IsEnabled = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
