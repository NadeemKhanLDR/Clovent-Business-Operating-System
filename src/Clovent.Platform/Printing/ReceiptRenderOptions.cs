namespace Clovent.Platform.Printing;

/// <summary>
/// Rendering configuration options for receipt text generation.
/// </summary>
public sealed record ReceiptRenderOptions(
    PaperWidth PaperWidth = PaperWidth.Width80mm,
    int CharactersPerLine = 42,
    bool IsReprint = false,
    int ReprintCount = 1,
    string? ReprintReason = null,
    string? HeaderText = "Clovent Business Operating System",
    string? FooterText = "Thank you for your visit!",
    ReceiptCopyType CopyType = ReceiptCopyType.Customer,
    int MarginMm = 2,
    string? LogoPath = null,
    bool EnableLogo = false);

/// <summary>
/// Result of a receipt formatting execution, indicating layout and script properties.
/// </summary>
public sealed record ReceiptFormattingResult(
    string FormattedText,
    bool ContainsComplexScript,
    int TotalLines);
