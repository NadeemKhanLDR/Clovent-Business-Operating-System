using System.Drawing.Printing;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Prints a <see cref="ReceiptFormatter"/>-produced plain-text receipt using
/// .NET's built-in <see cref="PrintDocument"/> - the standard, dependency-free
/// mechanism for POS/receipt printers (GDI text rendering works against any
/// installed Windows printer, including thermal receipt printers registered
/// as a standard print queue). Paginates automatically if a receipt runs
/// longer than one page's margin bounds.
/// </summary>
public sealed class ReceiptPrintDocument : PrintDocument
{
    private readonly string[] _lines;
    private int _nextLineIndex;

    /// <summary>Builds a print document for the given receipt text.</summary>
    public ReceiptPrintDocument(string receiptText)
    {
        DocumentName = "Receipt";
        _lines = receiptText.Replace("\r\n", "\n").Split('\n');
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

        using var font = new Font(FontFamily.GenericMonospace, 9f);
        var lineHeight = font.GetHeight(graphics);
        var y = (float)e.MarginBounds.Top;

        while (_nextLineIndex < _lines.Length)
        {
            if (y + lineHeight > e.MarginBounds.Bottom)
            {
                e.HasMorePages = true;
                return;
            }

            graphics.DrawString(_lines[_nextLineIndex], font, Brushes.Black, e.MarginBounds.Left, y);
            y += lineHeight;
            _nextLineIndex++;
        }

        e.HasMorePages = false;
    }
}
