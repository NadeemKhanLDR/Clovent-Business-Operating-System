using Clovent.Authentication.Credentials;
using Xunit;

namespace Clovent.Authentication.Tests.Credentials;

public class FailedAttemptsTests
{
    [Fact]
    public void Zero_HasCountZero()
    {
        Assert.Equal(0, FailedAttempts.Zero.Count);
    }

    [Fact]
    public void Increment_IncreasesCountByOne()
    {
        var attempts = FailedAttempts.Zero.Increment().Increment();

        Assert.Equal(2, attempts.Count);
    }

    [Fact]
    public void Increment_DoesNotMutateOriginal()
    {
        var original = FailedAttempts.Zero;

        original.Increment();

        Assert.Equal(0, original.Count);
    }

    [Fact]
    public void Reset_ReturnsToZero()
    {
        var attempts = FailedAttempts.Zero.Increment().Increment().Increment();

        var reset = attempts.Reset();

        Assert.Equal(0, reset.Count);
    }

    [Fact]
    public void MeetsOrExceeds_BelowThreshold_ReturnsFalse()
    {
        var attempts = FailedAttempts.Zero.Increment().Increment();

        Assert.False(attempts.MeetsOrExceeds(3));
    }

    [Fact]
    public void MeetsOrExceeds_AtThreshold_ReturnsTrue()
    {
        var attempts = FailedAttempts.Zero.Increment().Increment().Increment();

        Assert.True(attempts.MeetsOrExceeds(3));
    }

    [Fact]
    public void MeetsOrExceeds_NonPositiveThreshold_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FailedAttempts.Zero.MeetsOrExceeds(0));
    }

    [Fact]
    public void Equals_SameCount_AreEqual()
    {
        Assert.Equal(FailedAttempts.Zero.Increment(), FailedAttempts.Zero.Increment());
    }
}
