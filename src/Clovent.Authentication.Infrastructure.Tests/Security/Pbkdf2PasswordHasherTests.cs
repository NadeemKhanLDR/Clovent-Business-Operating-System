using Clovent.Authentication.Infrastructure.Security;
using Xunit;

namespace Clovent.Authentication.Infrastructure.Tests.Security;

public class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("correct horse battery staple");

        Assert.True(hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("correct horse battery staple");

        Assert.False(hasher.Verify("wrong password", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var first = hasher.Hash("password");
        var second = hasher.Hash("password");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalseRatherThanThrowing()
    {
        var hasher = new Pbkdf2PasswordHasher();

        Assert.False(hasher.Verify("password", "not-a-real-hash"));
    }
}
