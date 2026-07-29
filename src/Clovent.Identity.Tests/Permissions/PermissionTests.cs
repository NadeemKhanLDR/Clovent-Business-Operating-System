using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.Events;
using Clovent.Identity.Permissions.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Permissions;

public class PermissionTests
{
    [Fact]
    public void Create_Valid_RaisesPermissionCreated()
    {
        var permission = Permission.Create(PermissionCode.Create("identity.users.manage"), "Manage users");

        Assert.Equal("Manage users", permission.Description);
        Assert.IsType<PermissionCreated>(Assert.Single(permission.DomainEvents));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyDescription_Throws(string description)
    {
        Assert.Throws<ArgumentException>(() =>
            Permission.Create(PermissionCode.Create("identity.users.manage"), description));
    }

    [Fact]
    public void Create_DescriptionTooLong_Throws()
    {
        var tooLong = new string('a', 501);

        Assert.Throws<ArgumentException>(() =>
            Permission.Create(PermissionCode.Create("identity.users.manage"), tooLong));
    }
}
