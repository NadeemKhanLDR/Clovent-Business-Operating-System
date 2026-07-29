using Clovent.Authentication.Credentials;
using Xunit;

namespace Clovent.Authentication.Tests.Credentials;

public class SecurityStampTests
{
    [Fact]
    public void Generate_ProducesNonEmptyValue()
    {
        var stamp = SecurityStamp.Generate();

        Assert.False(string.IsNullOrWhiteSpace(stamp.Value));
    }

    [Fact]
    public void Generate_ProducesDifferentValuesEachTime()
    {
        var first = SecurityStamp.Generate();
        var second = SecurityStamp.Generate();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_Valid_Succeeds()
    {
        var stamp = SecurityStamp.Create("existing-stamp-value");

        Assert.Equal("existing-stamp-value", stamp.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Empty_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => SecurityStamp.Create(value));
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        Assert.Equal(SecurityStamp.Create("abc"), SecurityStamp.Create("abc"));
    }
}
