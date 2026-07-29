using Clovent.Domain.Tests.TestSupport;
using Xunit;

namespace Clovent.Domain.Tests;

public class EntityTests
{
    [Fact]
    public void Equals_SameTypeAndId_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_SameIdDifferentType_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new OtherTestEntity(id);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new TestEntity(Guid.NewGuid());

        Assert.False(a.Equals(null));
        Assert.False(a == null);
        Assert.True(a != null);
    }

    [Fact]
    public void GetHashCode_SameTypeAndId_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
