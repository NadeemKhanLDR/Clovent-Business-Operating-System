using System.Text;

namespace Clovent.Platform.Printing;

/// <summary>
/// Formats immutable receipt snapshot data into column-wrapped plain-text receipts
/// optimized for 58 mm (32 col), 80 mm (42-48 col), and A4 printer profiles.
/// Preserves immutable sale-time figures without recalculating taxes or totals.
/// Automatically detects Arabic/Urdu scripts for GDI+ raster rendering fallback.
/// </summary>
public static class ReceiptSnapshotFormatter
{
    /// <summary>
    /// Formats <paramref name="data"/> according to the given <paramref name="options"/>.
    /// </summary>
    public static ReceiptFormattingResult Format(PrintableReceiptData data, ReceiptRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);

        var charsPerLine = Math.Clamp(options.CharactersPerLine, 28, 120);
        var sb = new StringBuilder();
        var containsComplexScript = false;

        void AppendLineAndScan(string line)
        {
            if (!containsComplexScript && HasComplexScript(line))
            {
                containsComplexScript = true;
            }
            sb.AppendLine(line);
        }

        string Center(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            if (text.Length >= charsPerLine) return text;
            var pad = (charsPerLine - text.Length) / 2;
            return new string(' ', Math.Max(0, pad)) + text;
        }

        string TwoColumn(string left, string right)
        {
            var maxLeft = charsPerLine - right.Length - 1;
            if (maxLeft <= 0) return left + " " + right;
            if (left.Length > maxLeft) left = left[..maxLeft];
            var spaces = charsPerLine - left.Length - right.Length;
            return left + new string(' ', Math.Max(1, spaces)) + right;
        }

        var divider = new string('-', charsPerLine);
        var doubleDivider = new string('=', charsPerLine);

        // 1. Reprint Banner (if applicable)
        if (options.IsReprint)
        {
            AppendLineAndScan(doubleDivider);
            AppendLineAndScan(Center($"*** REPRINT (COPY #{options.ReprintCount}) ***"));
            if (!string.IsNullOrWhiteSpace(options.ReprintReason))
            {
                AppendLineAndScan(Center($"Reason: {options.ReprintReason}"));
            }
            AppendLineAndScan(Center($"Reprinted: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"));
            AppendLineAndScan(doubleDivider);
        }

        // 2. Header Information
        if (!string.IsNullOrWhiteSpace(options.HeaderText))
        {
            AppendLineAndScan(Center(options.HeaderText));
        }

        if (!string.IsNullOrWhiteSpace(data.OrganizationName))
        {
            AppendLineAndScan(Center(data.OrganizationName));
        }

        if (!string.IsNullOrWhiteSpace(data.BranchName))
        {
            AppendLineAndScan(Center(data.BranchName));
        }

        if (!string.IsNullOrWhiteSpace(data.TaxRegistrationNumber))
        {
            AppendLineAndScan(Center($"STRN / NTN: {data.TaxRegistrationNumber}"));
        }

        AppendLineAndScan(divider);

        // 3. Document Metadata
        AppendLineAndScan(TwoColumn("Order:", data.OrderNumber));
        if (data.DailySalesNumber.HasValue)
        {
            AppendLineAndScan(TwoColumn("Daily Sale #:", data.DailySalesNumber.Value.ToString()));
        }
        AppendLineAndScan(TwoColumn("Type:", data.OrderType));
        AppendLineAndScan(TwoColumn("Date:", data.CompletedAtUtc.ToString("yyyy-MM-dd HH:mm:ss")));

        if (!string.IsNullOrWhiteSpace(data.TerminalName))
        {
            AppendLineAndScan(TwoColumn("Terminal:", data.TerminalName));
        }

        if (!string.IsNullOrWhiteSpace(data.CashierName))
        {
            AppendLineAndScan(TwoColumn("Cashier:", data.CashierName));
        }

        AppendLineAndScan(divider);

        // 4. Line Items
        foreach (var item in data.Items)
        {
            var qtyPrice = $"x{item.Quantity:0.##} @ {item.UnitPrice:N2}";
            var totalStr = item.LineTotal.ToString("N2");

            // For narrow (e.g. 58mm) or long names, put item name on own line(s)
            if (item.Name.Length + qtyPrice.Length + totalStr.Length + 2 > charsPerLine)
            {
                AppendLineAndScan(item.Name);
                AppendLineAndScan(TwoColumn("  " + qtyPrice, totalStr));
            }
            else
            {
                var left = $"{item.Name} {qtyPrice}";
                AppendLineAndScan(TwoColumn(left, totalStr));
            }

            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                AppendLineAndScan($"  Note: {item.Notes}");
            }
        }

        AppendLineAndScan(divider);

        // 5. Totals Breakdown (Strictly Preserves Historical Values)
        AppendLineAndScan(TwoColumn("Subtotal:", data.Subtotal.ToString("N2")));

        if (data.DiscountTotal != 0m)
        {
            AppendLineAndScan(TwoColumn("Discount:", $"-{Math.Abs(data.DiscountTotal):N2}"));
        }

        if (data.TaxBreakdown != null && data.TaxBreakdown.Count > 0)
        {
            foreach (var tax in data.TaxBreakdown)
            {
                AppendLineAndScan(TwoColumn($"Tax ({tax.TaxName}):", tax.TaxAmount.ToString("N2")));
            }
        }
        else
        {
            AppendLineAndScan(TwoColumn("Tax:", data.TaxTotal.ToString("N2")));
        }

        if (data.ServiceChargeTotal != 0m)
        {
            AppendLineAndScan(TwoColumn("Service Charge:", data.ServiceChargeTotal.ToString("N2")));
        }

        AppendLineAndScan(doubleDivider);
        AppendLineAndScan(TwoColumn("Grand Total:", data.GrandTotal.ToString("N2")));
        AppendLineAndScan(doubleDivider);

        // 6. Payments Breakdown
        foreach (var payment in data.Payments)
        {
            AppendLineAndScan(TwoColumn($"Payment ({payment.PaymentMethodName}):", payment.Amount.ToString("N2")));
        }

        AppendLineAndScan(TwoColumn("Balance:", data.Balance.ToString("N2")));

        // 7. Customer Notes & Footer
        if (!string.IsNullOrWhiteSpace(data.CustomerNotes))
        {
            AppendLineAndScan(divider);
            AppendLineAndScan($"Notes: {data.CustomerNotes}");
        }

        if (!string.IsNullOrWhiteSpace(options.FooterText))
        {
            AppendLineAndScan(divider);
            AppendLineAndScan(Center(options.FooterText));
        }

        var text = sb.ToString();
        var lineCount = text.Split('\n').Length;

        return new ReceiptFormattingResult(text, containsComplexScript, lineCount);
    }

    /// <summary>
    /// Generates a standardized, diagnostic test slip for verifying printer connection and layout.
    /// </summary>
    public static string FormatTestPrintSlip(PrinterProfile profile, string operatorName)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var charsPerLine = Math.Clamp(profile.CharactersPerLine, 28, 120);
        var sb = new StringBuilder();

        string Center(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            if (text.Length >= charsPerLine) return text;
            var pad = (charsPerLine - text.Length) / 2;
            return new string(' ', Math.Max(0, pad)) + text;
        }

        string TwoColumn(string left, string right)
        {
            var maxLeft = charsPerLine - right.Length - 1;
            if (maxLeft <= 0) return left + " " + right;
            if (left.Length > maxLeft) left = left[..maxLeft];
            var spaces = charsPerLine - left.Length - right.Length;
            return left + new string(' ', Math.Max(1, spaces)) + right;
        }

        var divider = new string('=', charsPerLine);
        var subDivider = new string('-', charsPerLine);

        sb.AppendLine(divider);
        sb.AppendLine(Center("*** PRINTER DIAGNOSTIC TEST SLIP ***"));
        sb.AppendLine(divider);
        sb.AppendLine(TwoColumn("Profile Name:", profile.ProfileName));
        sb.AppendLine(TwoColumn("System Queue:", profile.SystemPrinterName));
        sb.AppendLine(TwoColumn("Role:", profile.Role.ToString()));
        sb.AppendLine(TwoColumn("Paper Width:", $"{(int)profile.PaperWidth} mm"));
        sb.AppendLine(TwoColumn("Columns:", profile.CharactersPerLine.ToString()));
        sb.AppendLine(TwoColumn("Cutter:", profile.SupportsCutter ? "Supported" : "None"));
        sb.AppendLine(TwoColumn("Drawer:", profile.SupportsCashDrawer ? "Supported" : "None"));
        sb.AppendLine(TwoColumn("Requested By:", operatorName));
        sb.AppendLine(TwoColumn("Test Time:", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"));
        sb.AppendLine(subDivider);
        sb.AppendLine(Center("CHARACTER RULER"));

        var ruler = new StringBuilder();
        for (int i = 1; i <= charsPerLine; i++)
        {
            ruler.Append((i % 10).ToString());
        }
        sb.AppendLine(ruler.ToString());
        sb.AppendLine(subDivider);
        sb.AppendLine(Center("URDU SCRIPT TEST:"));
        sb.AppendLine(Center("یہ ایک آزمائشی پرنٹ ہے"));
        sb.AppendLine(divider);
        sb.AppendLine(Center("HARDWARE TEST SUCCESSFUL"));
        sb.AppendLine(divider);

        return sb.ToString();
    }

    /// <summary>
    /// Returns true if the string contains characters in Arabic or Urdu Unicode blocks.
    /// </summary>
    public static bool HasComplexScript(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        foreach (var c in text)
        {
            // Arabic and Urdu Unicode blocks:
            // 0600 - 06FF: Arabic
            // 0750 - 077F: Arabic Supplement
            // 08A0 - 08FF: Arabic Extended-A
            // FB50 - FDFF: Arabic Presentation Forms-A
            // FE70 - FEFF: Arabic Presentation Forms-B
            if ((c >= 0x0600 && c <= 0x06FF) ||
                (c >= 0x0750 && c <= 0x077F) ||
                (c >= 0x08A0 && c <= 0x08FF) ||
                (c >= 0xFB50 && c <= 0xFDFF) ||
                (c >= 0xFE70 && c <= 0xFEFF))
            {
                return true;
            }
        }

        return false;
    }
}
