namespace Clovent.Restaurant.DomainServices;

/// <summary>Preserves proportional aging while reconciling its rounded buckets to the receivable.</summary>
public static class ProportionalAgingRounding
{
    /// <summary>
    /// Floors nonnegative bucket shares to cents, then distributes residual cents by largest
    /// fractional remainder. Equal remainders go to the older bucket first (higher index).
    /// Input order is Current, 1–7, 8–15, 16–30, 31–60, 60+ days.
    /// This changes presentation rounding, not debt allocation or ledger balances.
    /// </summary>
    public static decimal[] Reconcile(decimal receivable, params decimal[] shares)
    {
        if (receivable < 0m || shares.Any(x => x < 0m))
            throw new ArgumentOutOfRangeException(nameof(receivable));
        var target = MoneyRoundingPolicy.RoundMoney(receivable);
        if (MoneyRoundingPolicy.RoundMoney(shares.Sum()) != target)
            throw new InvalidOperationException("Aging shares do not reconcile before rounding.");
        var rounded = shares.Select(x => decimal.Floor(x * 100m) / 100m).ToArray();
        var remaining = (int)((target - rounded.Sum()) * 100m);
        var ranked = Enumerable.Range(0, shares.Length)
            .OrderByDescending(i => shares[i] - rounded[i]).ThenByDescending(i => i).ToArray();
        if (remaining < 0 || remaining > ranked.Length)
            throw new InvalidOperationException("Aging residual exceeds one cent per bucket.");
        for (var i = 0; i < remaining; i++) rounded[ranked[i]] += .01m;
        return rounded;
    }
}
