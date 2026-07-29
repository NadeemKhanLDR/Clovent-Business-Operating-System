using Clovent.MasterData.Shared.ValueObjects;
using Xunit;

namespace Clovent.MasterData.Tests.Shared;

public class EntityCodeTests
{
    [Fact]
    public void Create_LowercaseInput_NormalizesToUppercase()
    {
        var code = EntityCode.Create("wh-01");

        Assert.Equal("WH-01", code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("-ABC")]
    [InlineData("THIS-CODE-IS-WAY-TOO-LONG-01")]
    public void Create_Invalid_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => EntityCode.Create(value));
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        Assert.Equal(EntityCode.Create("WH-01"), EntityCode.Create("wh-01"));
    }
}
