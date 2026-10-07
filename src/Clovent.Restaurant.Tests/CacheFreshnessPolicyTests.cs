using Clovent.Restaurant.Continuity;
using Xunit;

namespace Clovent.Restaurant.Tests;

public sealed class CacheFreshnessPolicyTests
{
    [Fact]
    public void EvaluateFreshness_RecentCache_ReturnsValid()
    {
        var policy = CacheFreshnessPolicy.Default; // 12h warning, 48h max
        var now = DateTimeOffset.UtcNow;
        var generatedAt = now.AddHours(-2); // 2 hours old

        var status = policy.EvaluateFreshness(generatedAt, now);

        Assert.Equal(CacheValidationStatus.Valid, status);
    }

    [Fact]
    public void EvaluateFreshness_OlderThanWarningThreshold_ReturnsStaleWithinPolicy()
    {
        var policy = CacheFreshnessPolicy.Default;
        var now = DateTimeOffset.UtcNow;
        var generatedAt = now.AddHours(-16); // 16 hours old (> 12h, < 48h)

        var status = policy.EvaluateFreshness(generatedAt, now);

        Assert.Equal(CacheValidationStatus.StaleWithinPolicy, status);
    }

    [Fact]
    public void EvaluateFreshness_OlderThanMaxPermitted_ReturnsTooStale()
    {
        var policy = CacheFreshnessPolicy.Default;
        var now = DateTimeOffset.UtcNow;
        var generatedAt = now.AddHours(-49); // 49 hours old (> 48h)

        var status = policy.EvaluateFreshness(generatedAt, now);

        Assert.Equal(CacheValidationStatus.TooStale, status);
    }

    [Fact]
    public void EvaluateFreshness_SmallNegativeAgeClockDrift_ReturnsValid()
    {
        var policy = CacheFreshnessPolicy.Default;
        var now = DateTimeOffset.UtcNow;
        var generatedAt = now.AddMinutes(2); // 2 minutes in future (tolerable drift)

        var status = policy.EvaluateFreshness(generatedAt, now);

        Assert.Equal(CacheValidationStatus.Valid, status);
    }

    [Fact]
    public void EvaluateFreshness_ExcessiveFutureClockDrift_ReturnsCorrupt()
    {
        var policy = CacheFreshnessPolicy.Default;
        var now = DateTimeOffset.UtcNow;
        var generatedAt = now.AddMinutes(15); // 15 minutes in future (> 10m drift threshold)

        var status = policy.EvaluateFreshness(generatedAt, now);

        Assert.Equal(CacheValidationStatus.Corrupt, status);
    }

    [Fact]
    public void EvaluateFreshness_CustomPolicyThresholds_EvaluatesAccurately()
    {
        var customPolicy = new CacheFreshnessPolicy(
            WarningAge: TimeSpan.FromMinutes(30),
            MaxPermittedAge: TimeSpan.FromHours(2));

        var now = DateTimeOffset.UtcNow;

        Assert.Equal(CacheValidationStatus.Valid, customPolicy.EvaluateFreshness(now.AddMinutes(-20), now));
        Assert.Equal(CacheValidationStatus.StaleWithinPolicy, customPolicy.EvaluateFreshness(now.AddMinutes(-45), now));
        Assert.Equal(CacheValidationStatus.TooStale, customPolicy.EvaluateFreshness(now.AddMinutes(-150), now));
    }
}
