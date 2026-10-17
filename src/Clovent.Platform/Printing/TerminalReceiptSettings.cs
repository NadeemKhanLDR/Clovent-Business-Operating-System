namespace Clovent.Platform.Printing;

/// <summary>
/// Configuration for receipt copies, paper dimensions, and template branding
/// assigned to a specific terminal machine or used as the global default.
/// </summary>
public sealed class TerminalReceiptSettings
{
    /// <summary>Optional terminal machine identifier these settings apply to.</summary>
    public Guid? TerminalId { get; set; }

    /// <summary>Whether customer receipt copy printing is enabled.</summary>
    public bool CustomerCopyEnabled { get; set; } = true;

    /// <summary>Minimum supported copies per copy type.</summary>
    public const int MinCopyCount = 1;

    /// <summary>Maximum supported copies per copy type across UI, settings validation, and dispatch.</summary>
    public const int MaxCopyCount = 5;

    /// <summary>Destination printer profile ID for customer copy (null uses default receipt printer).</summary>
    public Guid? CustomerCopyProfileId { get; set; }

    /// <summary>Number of customer copies to produce (default 1, max 5).</summary>
    public int CustomerCopyCount { get; set; } = 1;

    /// <summary>Whether merchant/store accounting copy printing is enabled.</summary>
    public bool MerchantCopyEnabled { get; set; } = false;

    /// <summary>Destination printer profile ID for merchant copy (null uses customer copy destination).</summary>
    public Guid? MerchantCopyProfileId { get; set; }

    /// <summary>Number of merchant/store copies to produce (default 1, max 5).</summary>
    public int MerchantCopyCount { get; set; } = 1;

    /// <summary>
    /// Validates configured copy counts against the shared boundary limits (1 to 5).
    /// Returns false and an explicit validation error message if either copy count is out of range.
    /// </summary>
    public bool IsCopyCountValid(out string? errorMessage)
    {
        if (CustomerCopyEnabled && (CustomerCopyCount < MinCopyCount || CustomerCopyCount > MaxCopyCount))
        {
            errorMessage = $"Configured customer copy count ({CustomerCopyCount}) exceeds supported range ({MinCopyCount}-{MaxCopyCount}). Please correct the configuration.";
            return false;
        }

        if (MerchantCopyEnabled && (MerchantCopyCount < MinCopyCount || MerchantCopyCount > MaxCopyCount))
        {
            errorMessage = $"Configured merchant copy count ({MerchantCopyCount}) exceeds supported range ({MinCopyCount}-{MaxCopyCount}). Please correct the configuration.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>Paper width profile (58mm, 80mm, or A4).</summary>
    public PaperWidth PaperWidth { get; set; } = PaperWidth.Width80mm;

    /// <summary>Left and right margins in millimeters (0 to 20mm).</summary>
    public int MarginMm { get; set; } = 2;

    /// <summary>Monospace characters per line (typically 42 for 80mm, 32 for 58mm).</summary>
    public int CharactersPerLine { get; set; } = 42;

    /// <summary>Optional header text (e.g. business name, welcome greeting).</summary>
    public string? HeaderText { get; set; } = "Clovent Business Operating System";

    /// <summary>Optional footer text (e.g. thank you note, return policy, tax disclaimer).</summary>
    public string? FooterText { get; set; } = "Thank you for your visit!";

    /// <summary>Optional path to corporate logo image file on local storage.</summary>
    public string? LogoPath { get; set; }

    /// <summary>Whether corporate logo printing should be included on receipts.</summary>
    public bool EnableLogo { get; set; } = false;

    /// <summary>Whether paper cutter command should be executed at the end of printing.</summary>
    public bool SupportsCutter { get; set; } = true;

    /// <summary>Whether cash drawer kick command should be actuated on cash customer receipts.</summary>
    public bool SupportsCashDrawer { get; set; } = true;
}
