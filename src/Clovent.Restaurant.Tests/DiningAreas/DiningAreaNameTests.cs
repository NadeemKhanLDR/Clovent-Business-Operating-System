using Clovent.Restaurant.DiningAreas.ValueObjects;
using Xunit;

namespace Clovent.Restaurant.Tests.DiningAreas;

public class DiningAreaNameTests
{
    [Fact]
    public void Create_TrimsWhitespace()
    {
        var name = DiningAreaName.Create("  Patio  ");

        Assert.Equal("Patio", name.Value);
    }

    [Fact]
    public void Create_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => DiningAreaName.Create(""));
    }

    [Fact]
    public void Create_TooShort_Throws()
    {
        Assert.Throws<ArgumentException>(() => DiningAreaName.Create("A"));
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        Assert.Equal(DiningAreaName.Create("Patio"), DiningAreaName.Create("Patio"));
    }
}
