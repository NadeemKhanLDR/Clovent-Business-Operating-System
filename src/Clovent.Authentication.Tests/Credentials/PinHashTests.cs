using Clovent.Authentication.Credentials;
using Xunit;

namespace Clovent.Authentication.Tests.Credentials;

public class PinHashTests
{
    [Fact]
    public void Create_Valid_Succeeds()
    {
        var hash = PinHash.Create("hashed-pin-value");

        Assert.Equal("hashed-pin-value", hash.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Empty_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => PinHash.Create(value));
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        Assert.Equal(PinHash.Create("abc"), PinHash.Create("abc"));
    }

    [Fact]
    public void PinHashAndPasswordHash_AreDistinctTypes()
    {
        var pinHash = PinHash.Create("abc");
        var passwordHash = PasswordHash.Create("abc");

        Assert.NotEqual<object>(pinHash, passwordHash);
    }
}
