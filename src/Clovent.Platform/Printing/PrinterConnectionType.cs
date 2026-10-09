namespace Clovent.Platform.Printing;

/// <summary>
/// Hardware transport or operating system mechanism used to communicate with a printer.
/// </summary>
public enum PrinterConnectionType
{
    /// <summary>Standard Windows print spooler queue (driver-based).</summary>
    WindowsDriver = 0,

    /// <summary>Direct raw ESC/POS byte stream over local network TCP socket.</summary>
    EscPosNetwork = 1,

    /// <summary>Direct raw ESC/POS byte stream over serial COM / USB virtual COM port.</summary>
    EscPosSerial = 2,

    /// <summary>Virtual software printer generating PDF documents on disk.</summary>
    VirtualPdf = 3
}
