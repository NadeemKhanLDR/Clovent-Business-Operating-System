using Clovent.Authentication.Lockouts;
using Xunit;

namespace Clovent.Authentication.Tests.Lockouts;

public class LockoutPolicyTests
{
    [Fact]
    public void ShouldLock_BelowThreshold_ReturnsFalse()
    {
        var policy = LockoutPolicy.Create(5, TimeSpan.FromMinutes(15));

        Assert.False(policy.ShouldLock(4));
    }

    [Fact]
    public void ShouldLock_AtThreshold_ReturnsTrue()
    {
        var policy = LockoutPolicy.Create(5, TimeSpan.FromMinutes(15));

        Assert.True(policy.ShouldLock(5));
    }

    [Fact]
    public void ShouldLock_AboveThreshold_ReturnsTrue()
    {
        var policy = LockoutPolicy.Create(5, TimeSpan.FromMinutes(15));

        Assert.True(policy.ShouldLock(9));
    }

    [Fact]
    public void Create_MaxFailedAttemptsNotPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LockoutPolicy.Create(0, TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void Create_NonPositiveWindow_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LockoutPolicy.Create(5, TimeSpan.Zero));
    }

    [Fact]
    public void Equals_SameConfiguration_AreEqual()
    {
        var a = LockoutPolicy.Create(5, TimeSpan.FromMinutes(15));
        var b = LockoutPolicy.Create(5, TimeSpan.FromMinutes(15));

        Assert.Equal(a, b);
    }
}
