namespace Clovent.Restaurant.Application.OrderHealth;

/// <summary>
/// Pure, stateless evaluator mapping an open order's elapsed wait time to a
/// Green/Orange/Red status - deliberately free of any DI or clock dependency
/// so unit tests (and the Desktop Running Orders screen) can feed it any
/// elapsed value. Thresholds arrive as an <see cref="OrderHealthThresholds"/>
/// POCO the Desktop layer persists in its settings JSON; no database table
/// is involved.
/// </summary>
public static class OrderHealthEvaluator
{
    /// <summary>Evaluates health with the default thresholds (green 10 min, orange 20 min).</summary>
    public static OrderHealthResult Evaluate(TimeSpan elapsed) => Evaluate(elapsed, new OrderHealthThresholds());

    /// <summary>
    /// Evaluates health against the given thresholds. A negative elapsed
    /// time (clock skew) is treated as zero.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The thresholds fail <see cref="OrderHealthThresholds.Validate"/>.</exception>
    public static OrderHealthResult Evaluate(TimeSpan elapsed, OrderHealthThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);
        thresholds.Validate();

        var minutes = (int)Math.Max(0, elapsed.TotalMinutes);

        var status = minutes >= thresholds.OrangeMinutes
            ? OrderHealthStatus.Red
            : minutes >= thresholds.GreenMinutes
                ? OrderHealthStatus.Orange
                : OrderHealthStatus.Green;

        return new OrderHealthResult(status, OrderHealthResult.FormatDisplayText(minutes));
    }
}
