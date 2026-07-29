using Clovent.Identity.Organizations.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Organizations;

public class OrganizationNameTests
{
    [Fact]
    public void Create_Valid_Succeeds()
    {
        Assert.Equal("Acme Corp", OrganizationName.Create("Acme Corp").Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Create_TooShort_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => OrganizationName.Create(value));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        Assert.Throws<ArgumentException>(() => OrganizationName.Create(new string('a', 201)));
    }
}
