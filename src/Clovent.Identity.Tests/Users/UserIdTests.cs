using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Identity.Tests.Users;

public class UserIdTests
{
    [Fact]
    public void New_IsNotEmpty()
    {
        Assert.NotEqual(Guid.Empty, UserId.New().Value);
    }

    [Fact]
    public void Constructor_EmptyGuid_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UserId(Guid.Empty));
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        var value = Guid.NewGuid();

        Assert.Equal(new UserId(value), new UserId(value));
    }
}
