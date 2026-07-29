using Clovent.MasterData;
using Clovent.MasterData.Shared;
using Clovent.MasterData.TimeZones;
using Clovent.MasterData.TimeZones.Events;
using Xunit;

namespace Clovent.MasterData.Tests.TimeZones;

public class TimeZoneEntryTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesTimeZoneEntryCreated()
    {
        var entry = TimeZoneEntry.Create(IanaId.Create("America/New_York"), "(UTC-05:00) Eastern Time", -300);

        Assert.Equal("America/New_York", entry.IanaId.Value);
        Assert.Equal(-300, entry.UtcOffsetMinutes);
        Assert.Equal(MasterDataStatus.Active, entry.Status);
        Assert.IsType<TimeZoneEntryCreated>(Assert.Single(entry.DomainEvents));
    }

    [Theory]
    [InlineData(-900)]
    [InlineData(900)]
    public void Create_OffsetOutOfRange_Throws(int offsetMinutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TimeZoneEntry.Create(IanaId.Create("UTC"), "UTC", offsetMinutes));
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var entry = TimeZoneEntry.Create(IanaId.Create("UTC"), "UTC", 0);
        entry.Deactivate();

        Assert.Throws<MasterDataDomainException>(() => entry.Deactivate());
    }
}
