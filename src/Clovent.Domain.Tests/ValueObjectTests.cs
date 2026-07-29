using Clovent.Domain.Tests.TestSupport;
using Xunit;

namespace Clovent.Domain.Tests;

public class ValueObjectTests
{
    [Fact]
    public void Equals_SameComponents_ReturnsTrue()
    {
        var a = new TestValueObject("x", 1);
        var b = new TestValueObject("x", 1);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Equals_DifferentComponents_ReturnsFalse()
    {
        var a = new TestValueObject("x", 1);
        var b = new TestValueObject("x", 2);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var a = new TestValueObject("x", 1);
        var b = new OtherTestValueObject("x");

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new TestValueObject("x", 1);

        Assert.False(a.Equals(null));
        Assert.False(a == null);
    }

    [Fact]
    public void BothNull_AreEqual()
    {
        TestValueObject? a = null;
        TestValueObject? b = null;

        Assert.True(a == b);
    }

    [Fact]
    public void GetHashCode_SameComponents_AreEqual()
    {
        var a = new TestValueObject("x", 1);
        var b = new TestValueObject("x", 1);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
