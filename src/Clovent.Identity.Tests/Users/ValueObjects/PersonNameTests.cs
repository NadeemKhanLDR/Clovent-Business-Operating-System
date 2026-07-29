using Clovent.Identity.Users.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Users.ValueObjects;

public class PersonNameTests
{
    [Fact]
    public void Create_Valid_SetsFullName()
    {
        var name = PersonName.Create("Ada", "Lovelace");

        Assert.Equal("Ada", name.FirstName);
        Assert.Equal("Lovelace", name.LastName);
        Assert.Equal("Ada Lovelace", name.FullName);
    }

    [Theory]
    [InlineData("", "Lovelace")]
    [InlineData("Ada", "")]
    [InlineData("   ", "Lovelace")]
    public void Create_MissingPart_Throws(string first, string last)
    {
        Assert.Throws<ArgumentException>(() => PersonName.Create(first, last));
    }

    [Fact]
    public void Create_PartTooLong_Throws()
    {
        var tooLong = new string('a', 101);

        Assert.Throws<ArgumentException>(() => PersonName.Create(tooLong, "Lovelace"));
    }

    [Fact]
    public void Equals_SameParts_AreEqual()
    {
        var a = PersonName.Create("Ada", "Lovelace");
        var b = PersonName.Create("Ada", "Lovelace");

        Assert.Equal(a, b);
    }
}
