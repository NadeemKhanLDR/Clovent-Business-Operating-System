namespace Clovent.Restaurant.Continuity;

/// <summary>
/// Policy governing operational cache age thresholds for safe offline selling.
/// </summary>
public sealed record CacheFreshnessPolicy(
    TimeSpan WarningAge,
    TimeSpan MaxPermittedAge)
{
    /// <summary>Default conservative freshness policy: 12-hour warning, 48-hour hard expiry.</summary>
    public static readonly CacheFreshnessPolicy Default = new(
        WarningAge: TimeSpan.FromHours(12),
        MaxPermittedAge: TimeSpan.FromHours(48));

    /// <summary>
    /// Evaluates the freshness status of a cache generated at <paramref name="generatedAtUtc"/>.
    /// </summary>
    public CacheValidationStatus EvaluateFreshness(DateTimeOffset generatedAtUtc, DateTimeOffset nowUtc)
    {
        var age = nowUtc - generatedAtUtc;
        if (age < TimeSpan.Zero)
        {
            // Clock drift: Allow reasonable forward drift up to 10 minutes, otherwise treat as corrupt
            if (age < TimeSpan.FromMinutes(-10))
            {
                return CacheValidationStatus.Corrupt;
            }
            return CacheValidationStatus.Valid;
        }

        if (age > MaxPermittedAge)
        {
            return CacheValidationStatus.TooStale;
        }

        if (age > WarningAge)
        {
            return CacheValidationStatus.StaleWithinPolicy;
        }

        return CacheValidationStatus.Valid;
    }
}
