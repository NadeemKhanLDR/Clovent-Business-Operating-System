using Clovent.Desktop.Forms.Base;
using Xunit;

namespace Clovent.Desktop.Tests.Base;

public sealed class BusinessFormattersTests
{
    [Fact]
    public void BusinessDateTimeFormatter_FormatsCorrectly_With12Hour()
    {
        BusinessDateTimeFormatter.Configure("UTC", "dd-MMM-yyyy", "hh:mm tt");

        var testDate = new DateTimeOffset(2026, 10, 1, 18, 15, 0, TimeSpan.Zero);
        var formatted = BusinessDateTimeFormatter.Format(testDate);

        Assert.Equal("01-Oct-2026 06:15 PM", formatted);
    }

    [Fact]
    public void BusinessDateTimeFormatter_FormatsCorrectly_With24Hour()
    {
        BusinessDateTimeFormatter.Configure("UTC", "dd-MMM-yyyy", "HH:mm");

        var testDate = new DateTimeOffset(2026, 10, 1, 18, 15, 0, TimeSpan.Zero);
        var formatted = BusinessDateTimeFormatter.Format(testDate);

        Assert.Equal("01-Oct-2026 18:15", formatted);
    }

    [Fact]
    public void BusinessDateFormatter_FormatsDateOnly()
    {
        BusinessDateFormatter.Configure("UTC", "dd-MMM-yyyy");

        var testDate = new DateTimeOffset(2026, 10, 1, 18, 15, 0, TimeSpan.Zero);
        var formatted = BusinessDateFormatter.Format(testDate);

        Assert.Equal("01-Oct-2026", formatted);
    }

    [Fact]
    public void QuantityDisplay_Formats_WithTwoDecimalPlacesByDefault()
    {
        QuantityDisplay.Configure(2);

        Assert.Equal("145.00", QuantityDisplay.Format(145m));
        Assert.Equal("145.00", QuantityDisplay.Format(145.0000m));
        Assert.Equal("12.50", QuantityDisplay.Format(12.5m));
        Assert.Equal("0.00", QuantityDisplay.Format(0m));
    }

    [Fact]
    public void QuantityDisplay_Formats_WithConfiguredDecimals()
    {
        QuantityDisplay.Configure(3);
        Assert.Equal("145.000", QuantityDisplay.Format(145m));

        QuantityDisplay.Configure(0);
        Assert.Equal("146", QuantityDisplay.Format(145.8m));
        Assert.Equal("145", QuantityDisplay.Format(145.0m));

        // Reset back to standard 2
        QuantityDisplay.Configure(2);
    }
}
