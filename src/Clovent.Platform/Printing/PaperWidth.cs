namespace Clovent.Platform.Printing;

/// <summary>
/// Supported standard paper roll widths and document dimensions.
/// </summary>
public enum PaperWidth
{
    /// <summary>Standard 58 mm narrow thermal receipt roll (approx. 32 monospace characters).</summary>
    Width58mm = 58,

    /// <summary>Standard 80 mm wide thermal receipt roll (approx. 42 to 48 monospace characters).</summary>
    Width80mm = 80,

    /// <summary>Standard A4 or Letter sheet format (approx. 80+ monospace characters).</summary>
    A4Custom = 210
}
