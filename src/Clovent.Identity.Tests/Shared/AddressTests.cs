using Clovent.Identity.Shared.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Shared;

public class AddressTests
{
    [Fact]
    public void Create_Valid_SetsAllFields()
    {
        var address = Address.Create("1 Main St", "Springfield", "IL", "62704", "USA");

        Assert.Equal("1 Main St", address.Street);
        Assert.Equal("Springfield", address.City);
        Assert.Equal("IL", address.State);
        Assert.Equal("62704", address.PostalCode);
        Assert.Equal("USA", address.Country);
    }

    [Theory]
    [InlineData("", "Springfield", "IL", "62704", "USA")]
    [InlineData("1 Main St", "", "IL", "62704", "USA")]
    [InlineData("1 Main St", "Springfield", "", "62704", "USA")]
    [InlineData("1 Main St", "Springfield", "IL", "", "USA")]
    [InlineData("1 Main St", "Springfield", "IL", "62704", "")]
    public void Create_MissingField_Throws(string street, string city, string state, string postalCode, string country)
    {
        Assert.Throws<ArgumentException>(() => Address.Create(street, city, state, postalCode, country));
    }

    [Fact]
    public void Equals_SameFields_AreEqual()
    {
        var a = Address.Create("1 Main St", "Springfield", "IL", "62704", "USA");
        var b = Address.Create("1 Main St", "Springfield", "IL", "62704", "USA");

        Assert.Equal(a, b);
    }
}
