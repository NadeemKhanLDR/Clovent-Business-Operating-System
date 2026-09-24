namespace Clovent.Restaurant.Application.OrderHealth;

/// <summary>An order's evaluated health: the status plus a ready-to-render display text like "Waiting 12 min".</summary>
public sealed record OrderHealthResult(OrderHealthStatus Status, string DisplayText)
{
    /// <summary>Renders the display text for the given whole-minutes wait ("Waiting 12 min", clamped at zero).</summary>
    public static string FormatDisplayText(int minutes) => $"Waiting {Math.Max(0, minutes)} min";
}
