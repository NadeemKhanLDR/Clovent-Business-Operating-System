using Clovent.Identity.Users.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Users.ValueObjects;

public class UserNameTests
{
    [Theory]
    [InlineData("bob")]
    [InlineData("jane.doe")]
    [InlineData("user_name-42")]
    public void Create_ValidHandle_Succeeds(string value)
    {
        var userName = UserName.Create(value);

        Assert.Equal(value, userName.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("1startswithdigit")]
    [InlineData("has space")]
    [InlineData("has@symbol")]
    public void Create_InvalidHandle_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => UserName.Create(value));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        var value = "a" + new string('b', 40);

        Assert.Throws<ArgumentException>(() => UserName.Create(value));
    }

    [Fact]
    public void Equals_DifferentCase_AreEqual()
    {
        var a = UserName.Create("BobSmith");
        var b = UserName.Create("bobsmith");

        Assert.Equal(a, b);
    }
}
