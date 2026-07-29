using Clovent.Identity.Permissions.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Permissions;

public class PermissionCodeTests
{
    [Theory]
    [InlineData("identity.users.manage")]
    [InlineData("identity.users.read")]
    public void Create_Valid_Succeeds(string value)
    {
        var code = PermissionCode.Create(value);

        Assert.Equal(value, code.Value);
    }

    [Fact]
    public void Create_NormalizesCase()
    {
        var code = PermissionCode.Create("Identity.Users.Manage");

        Assert.Equal("identity.users.manage", code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("singleSegment")]
    [InlineData("Has Spaces.read")]
    [InlineData("trailing.dot.")]
    [InlineData(".leading.dot")]
    public void Create_Invalid_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => PermissionCode.Create(value));
    }

    [Fact]
    public void Equals_SameCode_AreEqual()
    {
        Assert.Equal(PermissionCode.Create("identity.users.read"), PermissionCode.Create("identity.users.read"));
    }
}
