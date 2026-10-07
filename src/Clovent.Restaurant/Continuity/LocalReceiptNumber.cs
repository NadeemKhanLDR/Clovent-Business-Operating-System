namespace Clovent.Restaurant.Continuity;

/// <summary>
/// Human-readable, terminal-scoped, collision-free receipt identifier for offline continuity transactions.
/// Format: <c>CONT-{TerminalCode}-{SequenceNumber:D5}</c> (e.g. <c>CONT-POS01-00042</c>).
/// </summary>
public static class LocalReceiptNumber
{
    private const string Prefix = "CONT";

    /// <summary>
    /// Generates a unique local receipt number scoped to the terminal and sequential transaction number.
    /// </summary>
    public static string Generate(string terminalCode, long sequenceNumber)
    {
        var sanitizedCode = SanitizeCode(terminalCode);
        return $"{Prefix}-{sanitizedCode}-{sequenceNumber:D5}";
    }

    /// <summary>
    /// Determines whether the given string represents a local continuity receipt identifier.
    /// </summary>
    public static bool IsContinuityReceiptNumber(string? receiptNumber)
    {
        if (string.IsNullOrWhiteSpace(receiptNumber)) return false;
        return receiptNumber.StartsWith(Prefix + "-", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to parse a local receipt number into its terminal code and sequence number.
    /// </summary>
    public static bool TryParse(string? receiptNumber, out string? terminalCode, out long sequenceNumber)
    {
        terminalCode = null;
        sequenceNumber = 0;

        if (string.IsNullOrWhiteSpace(receiptNumber)) return false;

        var parts = receiptNumber.Split('-');
        if (parts.Length < 3 || !string.Equals(parts[0], Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!long.TryParse(parts[^1], out sequenceNumber))
        {
            return false;
        }

        terminalCode = string.Join('-', parts.Skip(1).Take(parts.Length - 2));
        return !string.IsNullOrWhiteSpace(terminalCode);
    }

    private static string SanitizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "TERM";
        var clean = string.Concat(code.Trim().Where(char.IsLetterOrDigit)).ToUpperInvariant();
        return string.IsNullOrEmpty(clean) ? "TERM" : clean;
    }
}
