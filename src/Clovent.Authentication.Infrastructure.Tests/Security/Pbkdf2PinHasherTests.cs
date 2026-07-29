using Clovent.Authentication.Infrastructure.Security;
using Xunit;

namespace Clovent.Authentication.Infrastructure.Tests.Security;

public class Pbkdf2PinHasherTests
{
    [Fact]
    public void Hash_ThenVerify_WithCorrectPin_ReturnsTrue()
    {
        var hasher = new Pbkdf2PinHasher();
        var hash = hasher.Hash("482913");

        Assert.True(hasher.Verify("482913", hash));
    }

    [Fact]
    public void Verify_WithWrongPin_ReturnsFalse()
    {
        var hasher = new Pbkdf2PinHasher();
        var hash = hasher.Hash("482913");

        Assert.False(hasher.Verify("000000", hash));
    }
}
