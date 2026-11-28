using System;
using Clovent.Inventory.UnitsOfMeasure;
using Xunit;

namespace Clovent.Inventory.Tests.UnitsOfMeasure;

public class UnitConverterTests
{
    [Theory]
    [InlineData(1, "KG", "G", 1000)]
    [InlineData(500, "G", "KG", 0.5)]
    [InlineData(1, "LB", "OZ", 16)]
    [InlineData(32, "OZ", "LB", 2)]
    public void Convert_MassUnits_Accurate(decimal quantity, string fromUnit, string toUnit, decimal expected)
    {
        var converted = UnitConverter.Convert(quantity, fromUnit, toUnit);
        Assert.Equal(expected, converted);
    }

    [Theory]
    [InlineData(1, "L", "ML", 1000)]
    [InlineData(750, "ML", "L", 0.75)]
    [InlineData(1, "CUP", "ML", 240)]
    [InlineData(1, "TBSP", "TSP", 3)]
    [InlineData(1, "FL_OZ", "ML", 29.5735)]
    public void Convert_VolumeUnits_Accurate(decimal quantity, string fromUnit, string toUnit, decimal expected)
    {
        var converted = UnitConverter.Convert(quantity, fromUnit, toUnit);
        Assert.Equal(expected, converted);
    }

    [Theory]
    [InlineData(1, "DOZEN", "PCS", 12)]
    [InlineData(24, "PCS", "DOZEN", 2)]
    [InlineData(5, "EACH", "PCS", 5)]
    [InlineData(3, "PORTION", "PORTION", 3)]
    public void Convert_CountUnits_Accurate(decimal quantity, string fromUnit, string toUnit, decimal expected)
    {
        var converted = UnitConverter.Convert(quantity, fromUnit, toUnit);
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void Convert_IncompatibleCategories_ThrowsInvalidOperationException()
    {
        // Cannot convert Mass to Volume without density
        Assert.Throws<InvalidOperationException>(() =>
            UnitConverter.Convert(1m, "KG", "L"));

        // Cannot convert Count to Mass
        Assert.Throws<InvalidOperationException>(() =>
            UnitConverter.Convert(1m, "PCS", "G"));
    }

    [Fact]
    public void TryConvert_Incompatible_ReturnsFalse()
    {
        var success = UnitConverter.TryConvert(1m, "KG", "ML", out var converted);
        Assert.False(success);
        Assert.Equal(1m, converted);
    }

    [Fact]
    public void Convert_PrecisionAndAwayFromZeroRounding()
    {
        // 1/3 KG to G = 333.3333 G
        var converted = UnitConverter.Convert(0.33333333m, "KG", "G");
        Assert.Equal(333.3333m, converted);
    }
}
