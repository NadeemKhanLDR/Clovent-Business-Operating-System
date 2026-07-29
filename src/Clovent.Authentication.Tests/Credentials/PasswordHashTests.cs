using Clovent.Authentication.Credentials;
using Xunit;

namespace Clovent.Authentication.Tests.Credentials;

public class PasswordHashTests
{
    [Fact]
    public void Create_Valid_Succeeds()
    {
        var hash = PasswordHash.Create("$2a$11$abcdefghijklmnopqrstuv");

        Assert.Equal("$2a$11$abcdefghijklmnopqrstuv", hash.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Empty_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => PasswordHash.Create(value));
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        Assert.Equal(PasswordHash.Create("abc"), PasswordHash.Create("abc"));
    }

    [Fact]
    public void Equals_DifferentValue_AreNotEqual()
    {
        Assert.NotEqual(PasswordHash.Create("abc"), PasswordHash.Create("xyz"));
    }
}
