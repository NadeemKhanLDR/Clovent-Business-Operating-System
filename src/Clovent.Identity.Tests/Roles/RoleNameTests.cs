using Clovent.Identity.Roles.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Roles;

public class RoleNameTests
{
    [Fact]
    public void Create_Valid_Succeeds()
    {
        var name = RoleName.Create("Branch Manager");

        Assert.Equal("Branch Manager", name.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Create_TooShort_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => RoleName.Create(value));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        Assert.Throws<ArgumentException>(() => RoleName.Create(new string('a', 65)));
    }

    [Fact]
    public void Equals_DifferentCase_AreEqual()
    {
        Assert.Equal(RoleName.Create("Manager"), RoleName.Create("manager"));
    }
}
