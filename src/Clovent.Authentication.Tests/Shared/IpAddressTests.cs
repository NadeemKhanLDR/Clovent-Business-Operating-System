using Clovent.Authentication.Shared.ValueObjects;
using Xunit;

namespace Clovent.Authentication.Tests.Shared;

public class IpAddressTests
{
    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("::1")]
    [InlineData("2001:db8::ff00:42:8329")]
    public void Create_Valid_Succeeds(string value)
    {
        var ip = IpAddress.Create(value);

        Assert.NotNull(ip.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-ip")]
    [InlineData("999.999.999.999")]
    public void Create_Invalid_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => IpAddress.Create(value));
    }

    [Fact]
    public void Equals_SameAddress_AreEqual()
    {
        Assert.Equal(IpAddress.Create("10.0.0.1"), IpAddress.Create("10.0.0.1"));
    }
}
