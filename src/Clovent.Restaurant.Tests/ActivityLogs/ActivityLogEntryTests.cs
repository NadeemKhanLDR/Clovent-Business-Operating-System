using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.ActivityLogs.Events;
using Xunit;

namespace Clovent.Restaurant.Tests.ActivityLogs;

public class ActivityLogEntryTests
{
    [Fact]
    public void Record_Valid_SetsFieldsAndRaisesActivityLogEntryRecorded()
    {
        var entry = ActivityLogEntry.Record("Payment", "Rs.850.00 via Cash", "Ali Khan", "POS-01");

        Assert.Equal("Payment", entry.Action);
        Assert.Equal("Rs.850.00 via Cash", entry.Details);
        Assert.Equal("Ali Khan", entry.PerformedBy);
        Assert.Equal("POS-01", entry.MachineName);
        Assert.IsType<ActivityLogEntryRecorded>(Assert.Single(entry.DomainEvents));
    }

    [Fact]
    public void Record_NullOrWhitespaceDetails_NormalizesToNull()
    {
        var entry = ActivityLogEntry.Record("Print", "   ", "Ali Khan", "POS-01");

        Assert.Null(entry.Details);
    }

    [Fact]
    public void Record_EmptyAction_Throws()
    {
        Assert.Throws<ArgumentException>(() => ActivityLogEntry.Record("", null, "Ali Khan", "POS-01"));
    }

    [Fact]
    public void Record_EmptyPerformedBy_Throws()
    {
        Assert.Throws<ArgumentException>(() => ActivityLogEntry.Record("Print", null, "", "POS-01"));
    }

    [Fact]
    public void Record_DetailsExceedingMaxLength_Truncates()
    {
        var longDetails = new string('x', 2000);

        var entry = ActivityLogEntry.Record("Print", longDetails, "Ali Khan", "POS-01");

        Assert.Equal(1000, entry.Details!.Length);
    }
}
