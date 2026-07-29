using Clovent.Identity.Users.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Users.ValueObjects;

public class DisplayNameTests
{
    [Fact]
    public void Create_Valid_Succeeds()
    {
        var name = DisplayName.Create("  Ada  ");

        Assert.Equal("Ada", name.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Empty_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => DisplayName.Create(value));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        var tooLong = new string('a', DisplayName.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => DisplayName.Create(tooLong));
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        Assert.Equal(DisplayName.Create("Ada"), DisplayName.Create("Ada"));
    }

    [Fact]
    public void Equals_DifferentValue_AreNotEqual()
    {
        Assert.NotEqual(DisplayName.Create("Ada"), DisplayName.Create("Grace"));
    }
}
