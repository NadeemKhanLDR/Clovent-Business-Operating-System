using Clovent.Catalog.Shared.ValueObjects;
using Xunit;

namespace Clovent.Catalog.Tests.Shared;

public class SkuTests
{
    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("-ABC")]
    public void Create_Invalid_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => Sku.Create(value));
    }

    [Fact]
    public void Create_Valid_Normalizes()
    {
        Assert.Equal("ESP-1KG", Sku.Create("esp-1kg").Value);
    }
}
