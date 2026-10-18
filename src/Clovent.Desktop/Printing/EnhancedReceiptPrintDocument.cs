using System.Drawing;
using System.Drawing.Printing;
using Clovent.Platform.Printing;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Enhanced GDI+ print document that renders column-wrapped plain-text receipts
/// to thermal printer queues (58 mm, 80 mm) and A4 document queues.
/// Implements automatic multi-page pagination and high-fidelity GDI+ raster rendering
/// for Urdu and complex scripts where hardware character ROMs are unavailable.
/// </summary>
public sealed class EnhancedReceiptPrintDocument : PrintDocument
{
    private readonly string[] _lines;
    private readonly PaperWidth _paperWidth;
    private readonly bool _containsComplexScript;
    private readonly int _marginMm;
    private readonly string? _logoPath;
    private int _nextLineIndex;
    private bool _logoDrawn;

    /// <summary>
    /// Constructs an enhanced receipt print document with physical logo and custom margin support.
    /// </summary>
    /// <param name="receiptText">The formatted plain text to render.</param>
    /// <param name="paperWidth">Target paper roll width (58mm, 80mm, A4).</param>
    /// <param name="containsComplexScript">Whether the text includes Urdu/Arabic Unicode characters.</param>
    /// <param name="marginMm">Physical page margin in millimeters.</param>
    /// <param name="logoPath">Optional path to store logo image to physically render via GDI+.</param>
    public EnhancedReceiptPrintDocument(
        string receiptText,
        PaperWidth paperWidth = PaperWidth.Width80mm,
        bool containsComplexScript = false,
        int marginMm = 2,
        string? logoPath = null)
    {
        DocumentName = "CBOS Receipt";
        _lines = (receiptText ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        _paperWidth = paperWidth;
        _containsComplexScript = containsComplexScript;
        _marginMm = Math.Clamp(marginMm, 0, 25);
        _logoPath = logoPath;

        ConfigureDefaultPageSettings();
    }

    private void ConfigureDefaultPageSettings()
    {
        try
        {
            // Convert margin in millimeters to hundredths of an inch (1 mm = 3.937 / 100 in)
            int marginHundreds = (int)Math.Round(_marginMm * 3.93701);
            marginHundreds = Math.Clamp(marginHundreds, 0, 100);

            DefaultPageSettings.Margins = new Margins(marginHundreds, marginHundreds, marginHundreds, marginHundreds);

            int widthHundreds = _paperWidth switch
            {
                PaperWidth.Width58mm => 228, // ~58mm in 1/100 inches
                PaperWidth.Width80mm => 315, // ~80mm in 1/100 inches
                PaperWidth.A4Custom => 827,  // 210mm in 1/100 inches
                _ => 315
            };

            // Custom roll size with continuous length
            DefaultPageSettings.PaperSize = new PaperSize("ReceiptRoll", widthHundreds, 2000);
        }
        catch
        {
            // Fallback to standard driver paper size
        }
    }

    /// <inheritdoc/>
    protected override void OnBeginPrint(PrintEventArgs e)
    {
        base.OnBeginPrint(e);
        _nextLineIndex = 0;
        _logoDrawn = false;
    }

    /// <inheritdoc/>
    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        if (e.Graphics is not { } graphics)
        {
            return;
        }

        // High quality raster hints
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

        // Fonts: monospace for numbers/tables to maintain rigid column alignment, Segoe UI for complex/Urdu shaping
        using var monoFont = new Font(FontFamily.GenericMonospace, 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var complexFont = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);

        var lineHeight = Math.Max(monoFont.GetHeight(graphics), complexFont.GetHeight(graphics)) + 1.5f;
        var y = (float)e.MarginBounds.Top;
        var left = (float)e.MarginBounds.Left;
        var width = (float)e.MarginBounds.Width;

        using var brush = new SolidBrush(Color.Black);
        using var rtlFormat = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

        // 1. Physically render header logo image at top of first page if configured
        if (!_logoDrawn && !string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath))
        {
            try
            {
                using var logoImg = Image.FromFile(_logoPath);
                float maxLogoWidth = Math.Min(width, 160f); // Max ~40mm
                float maxLogoHeight = 70f;
                float scale = Math.Min(maxLogoWidth / logoImg.Width, maxLogoHeight / logoImg.Height);
                float destWidth = logoImg.Width * scale;
                float destHeight = logoImg.Height * scale;
                float destX = left + (width - destWidth) / 2f; // Centered

                graphics.DrawImage(logoImg, destX, y, destWidth, destHeight);
                y += destHeight + 6f; // Spacing after logo
            }
            catch
            {
                // Fallback gracefully if logo file fails to load
            }
            _logoDrawn = true;
        }

        while (_nextLineIndex < _lines.Length)
        {
            if (y + lineHeight > e.MarginBounds.Bottom)
            {
                e.HasMorePages = true;
                return;
            }

            var line = _lines[_nextLineIndex];

            // If the physical logo was rendered above, suppress the text placeholder notice
            if (_logoDrawn && line.Trim().StartsWith("[") && line.Trim().EndsWith("LOGO]"))
            {
                _nextLineIndex++;
                continue;
            }

            var rect = new RectangleF(left, y, width, lineHeight);

            if (ReceiptSnapshotFormatter.HasComplexScript(line))
            {
                // Urdu / Arabic line: render with complex font and RTL direction
                graphics.DrawString(line, complexFont, brush, rect, rtlFormat);
            }
            else
            {
                // Monospace alignment for table headers, columns, dividers, and money figures
                graphics.DrawString(line, monoFont, brush, left, y);
            }

            y += lineHeight;
            _nextLineIndex++;
        }

        e.HasMorePages = false;
    }
}
