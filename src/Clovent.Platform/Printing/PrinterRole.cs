namespace Clovent.Platform.Printing;

/// <summary>
/// Identifies the operational role and document type handled by a printer.
/// </summary>
public enum PrinterRole
{
    /// <summary>Customer payment receipt and tender breakdown.</summary>
    Receipt = 0,

    /// <summary>A4 or letter tax invoice for wholesale or corporate customers.</summary>
    Invoice = 1,

    /// <summary>Food order preparation ticket routed to kitchen prep stations.</summary>
    Kitchen = 2,

    /// <summary>Beverage order preparation ticket routed to bar station.</summary>
    Bar = 3,

    /// <summary>Adhesive barcode or shelf-edge label.</summary>
    Label = 4,

    /// <summary>Back-office shift summary or daily audit report.</summary>
    Report = 5
}
