namespace Clovent.Platform.Printing;

/// <summary>
/// Bitwise flags representing the physical condition of a thermal receipt or document printer.
/// Enables multi-fault diagnostics such as CoverOpen combined with PaperOut.
/// </summary>
[Flags]
public enum PrinterHardwareCondition
{
    /// <summary>Printer hardware is operational and ready to accept print jobs.</summary>
    Normal = 0,

    /// <summary>Printer is powered off, disconnected from port/USB, or network-unreachable.</summary>
    Offline = 1 << 0,

    /// <summary>Paper roll is exhausted (out of paper).</summary>
    PaperOut = 1 << 1,

    /// <summary>Printer cover, lid, or roll door is open.</summary>
    CoverOpen = 1 << 2,

    /// <summary>Auto-cutter blade is jammed, blocked, or experienced a mechanical error.</summary>
    CutterError = 1 << 3,

    /// <summary>Paper feed mechanism is jammed.</summary>
    PaperJam = 1 << 4,

    /// <summary>General hardware fault or unclassified printer error reported by driver.</summary>
    GeneralError = 1 << 5,

    /// <summary>Printer queue is paused by operating system or operator.</summary>
    Paused = 1 << 6,

    /// <summary>Output bin or paper exit tray is full.</summary>
    OutputBinFull = 1 << 7
}
