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
    private int _nextLineIndex;

    /// <summary>
    /// Constructs an enhanced receipt print document.
    /// </summary>
    /// <param name="receiptText">The formatted plain text to render.</param>
    /// <param name="paperWidth">Target paper roll width (58mm, 80mm, A4).</param>
    /// <param name="containsComplexScript">Whether the text includes Urdu/Arabic Unicode characters.</param>
    public EnhancedReceiptPrintDocument(string receiptText, PaperWidth paperWidth = PaperWidth.Width80mm, bool containsComplexScript = false)
    {
        DocumentName = "CBOS Receipt";
        _lines = (receiptText ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        _paperWidth = paperWidth;
        _containsComplexScript = containsComplexScript;

        ConfigureDefaultPageSettings();
    }

    private void ConfigureDefaultPageSettings()
    {
        try
        {
            DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);

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
    }

    /// <inheritdoc/>
    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        if (e.Graphics is not { } graphics)
        {
            return;
        }

        // Use high-quality rendering for crisp thermal print
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

        // Choose appropriate font: Segoe UI or Consolas for complex script vs GenericMonospace
        using var font = _containsComplexScript
            ? new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point)
            : new Font(FontFamily.GenericMonospace, 8.5f, FontStyle.Regular, GraphicsUnit.Point);

        var lineHeight = font.GetHeight(graphics) + 1.5f;
        var y = (float)e.MarginBounds.Top;
        var left = (float)e.MarginBounds.Left;
        var width = (float)e.MarginBounds.Width;

        using var brush = new SolidBrush(Color.Black);
        using var leftFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        using var centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var rightFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        using var rtlFormat = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

        while (_nextLineIndex < _lines.Length)
        {
            if (y + lineHeight > e.MarginBounds.Bottom)
            {
                e.HasMorePages = true;
                return;
            }

            var line = _lines[_nextLineIndex];
            var rect = new RectangleF(left, y, width, lineHeight);

            if (ReceiptSnapshotFormatter.HasComplexScript(line))
            {
                // Urdu / Arabic line: render with RTL direction flag
                graphics.DrawString(line, font, brush, rect, rtlFormat);
            }
            else
            {
                // Monospace alignment
                graphics.DrawString(line, font, brush, left, y);
            }

            y += lineHeight;
            _nextLineIndex++;
        }

        e.HasMorePages = false;
    }
}
