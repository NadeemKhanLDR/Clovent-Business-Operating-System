using Clovent.Identity.Users.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Users.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last@sub.example.co")]
    public void Create_ValidAddress_Succeeds(string value)
    {
        var email = Email.Create(value);

        Assert.Equal(value.ToLowerInvariant(), email.Value);
    }

    [Fact]
    public void Create_NormalizesCaseAndWhitespace()
    {
        var email = Email.Create("  User@Example.COM  ");

        Assert.Equal("user@example.com", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    [InlineData("no-at-sign.com")]
    public void Create_InvalidAddress_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => Email.Create(value));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        var value = new string('a', 250) + "@example.com";

        Assert.Throws<ArgumentException>(() => Email.Create(value));
    }

    [Fact]
    public void Equals_SameAddressDifferentCase_AreEqual()
    {
        var a = Email.Create("user@example.com");
        var b = Email.Create("USER@EXAMPLE.COM");

        Assert.Equal(a, b);
    }
}
