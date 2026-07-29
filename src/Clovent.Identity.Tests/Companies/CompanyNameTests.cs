using Clovent.Identity.Companies.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Companies;

public class CompanyNameTests
{
    [Fact]
    public void Create_Valid_Succeeds()
    {
        Assert.Equal("Acme Retail", CompanyName.Create("Acme Retail").Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Create_TooShort_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => CompanyName.Create(value));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        Assert.Throws<ArgumentException>(() => CompanyName.Create(new string('a', 201)));
    }
}
