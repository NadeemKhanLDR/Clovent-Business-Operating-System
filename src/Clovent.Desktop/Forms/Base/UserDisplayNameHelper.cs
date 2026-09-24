using System;
using Clovent.Desktop.Sessions;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Provides consistent resolution and formatting for cashier and user display names across the POS and shift dialogs.
/// </summary>
public static class UserDisplayNameHelper
{
    /// <summary>
    /// Gets the display name for the current session or a fallback string.
    /// Prefers non-empty <see cref="ICurrentSession.DisplayName"/>, falls back to <paramref name="fallback"/>.
    /// </summary>
    public static string GetCurrentCashierDisplayName(ICurrentSession? session, string fallback = "Cashier")
    {
        if (session != null && !string.IsNullOrWhiteSpace(session.DisplayName))
        {
            return session.DisplayName.Trim();
        }
        return fallback;
    }

    /// <summary>
    /// Formats the cashier name for display, ensuring empty or whitespace strings fallback to a clean label.
    /// </summary>
    public static string FormatCashierName(string? cashierName, string fallback = "Cashier")
    {
        if (!string.IsNullOrWhiteSpace(cashierName))
        {
            return cashierName.Trim();
        }
        return fallback;
    }
}
